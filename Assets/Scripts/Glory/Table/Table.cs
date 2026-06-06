using System.Collections.Generic;
using UnityEngine;

public interface ITable
{

}

public class Table<T> : ITable where T : Record, new()
{
	private List<T> m_list = new List<T>();

	public List<T> list => m_list;

	public Table(List<T> listRecord)
	{
		m_list = listRecord;
	}

}

public class Record
{
	public int Id;
}
