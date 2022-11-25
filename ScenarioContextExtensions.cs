using System;
using System.Data.SqlClient;
using System.IO;
using TechTalk.SpecFlow;

namespace SeleniumBot
{
	public static class ScenarioContextExtensions
	{
		public static void AddOrUpdate(this ScenarioContext _scenarioContext, string key, object value) 
		{
			if (_scenarioContext.ContainsKey(key))
			{
				_scenarioContext.Remove(key);
				Console.WriteLine($"Updated {key} to {value}");
			}
			else Console.WriteLine($"Added {key} of {value} to context");
			_scenarioContext.Add(key, value);
		}
		static string _connectionString => Environment.GetEnvironmentVariable("TwitchDBConn");

		public static SqlConnection Con(this ScenarioContext _scenarioContext)
		{
			if (_scenarioContext.TryGetValue("SqlConn", out SqlConnection conn)) return conn;
			var newCon = new SqlConnection(_connectionString);
			newCon.Open();
			_scenarioContext.Add("SqlConn", newCon); //Hard add to force you to keep this clean, ensuring all open conns get closed
			return newCon;
		}
		public static void CloseSqlConn(this ScenarioContext _scenarioContext)
		{
			if (_scenarioContext.ContainsKey("SqlConn"))
			{
				_scenarioContext.Con().Close();
				_scenarioContext.Remove("SqlConn");
			}
		}
		public static void CloseStreamWriterConn(this ScenarioContext _scenarioContext)
		{
			var writer = _scenarioContext.Get<StreamWriter>("StreamWriter");
			var reader = _scenarioContext.Get<StreamReader>("StreamReader");
			writer.Flush();
			writer.Close();
			reader.Close();
			_scenarioContext.Remove("StreamWriter");
			_scenarioContext.Remove("StreamReader");
		}
	}
}
