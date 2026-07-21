using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Unity MCP가 메뉴 아이템(execute_menu_item)으로 호출할 QA 녹화 진입점.
// Unity Recorder(RecorderController/MovieRecorderSettings) 기반이었으나, 그 방식은 녹화 중
// Play Mode 실시간 재생 자체를 강제로 늦추는 게 구조적 한계라(RecordingSession.cs 확인) 폐기.
// 대신 EditorApplication.update마다 ScreenCapture.CaptureScreenshot로 PNG만 저장(게임 속도에
// 전혀 영향 없음) → StopRecording 시 ffmpeg로 mp4 스티칭.
public static class QARecorder
{
    private const string OUTPUT_FOLDER_NAME = "QA_Recordings";
    private const float CAPTURE_FRAME_RATE = 60f;

    private static bool m_isRecording;
    private static string m_SessionName;
    private static string m_FrameFolder;
    private static int m_FrameCount;
    private static float m_StartRealTime;
    private static float m_LastCaptureRealTime;

    [MenuItem("Tools/QA/Start Recording")]
    public static void StartRecording()
    {
        if (m_isRecording == true)
        {
            Debug.LogWarning($"[QARecorder] StartRecording Skipped! 이미 녹화 중입니다.");
            return;
        }

        m_SessionName = $"qa_{DateTime.Now:yyyyMMdd_HHmmss}";
        m_FrameFolder = Path.Combine(GetOutputFolder(), m_SessionName + "_frames");
        Directory.CreateDirectory(m_FrameFolder);

        m_FrameCount = 0;
        m_StartRealTime = Time.realtimeSinceStartup;
        m_LastCaptureRealTime = -1f;
        m_isRecording = true;

        EditorApplication.update += CaptureFrame;

        WriteMarkerFile("recording_started", null);

        Debug.Log($"[QARecorder] StartRecording! frames: {m_FrameFolder}");
    }

    [MenuItem("Tools/QA/Stop Recording")]
    public static void StopRecording()
    {
        if (m_isRecording == false)
        {
            Debug.LogWarning($"[QARecorder] StopRecording Skipped! 진행 중인 녹화가 없습니다.");
            return;
        }

        EditorApplication.update -= CaptureFrame;
        m_isRecording = false;

        float elapsed = Time.realtimeSinceStartup - m_StartRealTime;
        float fps = (elapsed > 0f && m_FrameCount > 0) ? (m_FrameCount / elapsed) : CAPTURE_FRAME_RATE;

        string mp4Path = StitchFrames(fps);
        WriteMarkerFile("recording_stopped", mp4Path);

        if (mp4Path != null)
            Debug.Log($"[QARecorder] StopRecording Complete! frames: {m_FrameCount}, fps: {fps:F2}, output: {mp4Path}");
        else
            Debug.LogError($"[QARecorder] StopRecording! ffmpeg 스티칭 실패 — PNG 시퀀스만 남음: {m_FrameFolder}");
    }

    private static void CaptureFrame()
    {
        if (Application.isPlaying == false)
            return;

        float now = Time.realtimeSinceStartup;
        if (m_LastCaptureRealTime >= 0f && now - m_LastCaptureRealTime < 1f / CAPTURE_FRAME_RATE)
            return;

        m_LastCaptureRealTime = now;

        string framePath = Path.Combine(m_FrameFolder, $"frame_{m_FrameCount:D5}.png");
        ScreenCapture.CaptureScreenshot(framePath);
        m_FrameCount++;
    }

    private static string StitchFrames(float _fps)
    {
        if (m_FrameCount == 0)
        {
            Debug.LogWarning($"[QARecorder] StitchFrames Skipped! 캡처된 프레임이 없습니다.");
            return null;
        }

        string ffmpegPath = ResolveFFmpegPath();
        if (ffmpegPath == null)
        {
            Debug.LogError($"[QARecorder] StitchFrames Failed! ffmpeg를 찾을 수 없습니다 (PATH 또는 %LOCALAPPDATA%\\ffmpeg 확인).");
            return null;
        }

        string mp4Path = Path.Combine(GetOutputFolder(), m_SessionName + ".mp4");
        string framePattern = Path.Combine(m_FrameFolder, "frame_%05d.png");
        string fpsArg = _fps.ToString("F3", CultureInfo.InvariantCulture);
        string arguments = $"-y -framerate {fpsArg} -i \"{framePattern}\" -pix_fmt yuv420p \"{mp4Path}\"";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };

        using (Process process = Process.Start(startInfo))
        {
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Debug.LogError($"[QARecorder] ffmpeg 종료 코드 {process.ExitCode}\n{stderr}");
                return null;
            }
        }

        Directory.Delete(m_FrameFolder, true);

        return mp4Path;
    }

    private static string ResolveFFmpegPath()
    {
        string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (string directory in pathEnv.Split(Path.PathSeparator))
        {
            string candidate = Path.Combine(directory, "ffmpeg.exe");
            if (File.Exists(candidate) == true)
                return candidate;
        }

        string localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrEmpty(localAppData) == true)
            return null;

        string ffmpegRoot = Path.Combine(localAppData, "ffmpeg");
        if (Directory.Exists(ffmpegRoot) == false)
            return null;

        foreach (string versionFolder in Directory.GetDirectories(ffmpegRoot))
        {
            string candidate = Path.Combine(versionFolder, "bin", "ffmpeg.exe");
            if (File.Exists(candidate) == true)
                return candidate;
        }

        return null;
    }

    private static string GetOutputFolder()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string outputFolder = Path.Combine(projectRoot, OUTPUT_FOLDER_NAME);

        if (Directory.Exists(outputFolder) == false)
            Directory.CreateDirectory(outputFolder);

        return outputFolder;
    }

    private static void WriteMarkerFile(string _status, string _outputPath)
    {
        string markerPath = Path.Combine(GetOutputFolder(), "last_recording.json");
        string escapedPath = (_outputPath == null) ? null : _outputPath.Replace("\\", "\\\\");
        string pathField = (escapedPath == null) ? "null" : $"\"{escapedPath}\"";
        string json = $"{{\"status\":\"{_status}\",\"path\":{pathField},\"frameCount\":{m_FrameCount},\"timestamp\":\"{DateTime.Now:O}\"}}";
        File.WriteAllText(markerPath, json);
    }
}
