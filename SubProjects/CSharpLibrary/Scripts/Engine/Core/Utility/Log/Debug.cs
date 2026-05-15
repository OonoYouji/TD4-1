using System.Runtime.CompilerServices;
using System.IO;

public enum LogCategory {
	Engine,
	ScriptEngine,
	Application
}

static public class Debug {
	static public void Log(string message, [CallerFilePath] string filePath = "") {
#if DEBUG
		InternalConsoleLog(message, DetermineCategory(filePath));
#endif
	}

	static public void LogInfo(string message, [CallerFilePath] string filePath = "") {
		Log(message, filePath);
	}

	static public void LogWarning(string message, [CallerFilePath] string filePath = "") {
#if DEBUG
		InternalConsoleLog(message, DetermineCategory(filePath));
#endif
	}

	static public void LogError(string message, [CallerFilePath] string filePath = "") {
#if DEBUG
		InternalConsoleLog(message, DetermineCategory(filePath));
#endif
	}

	/// <summary>
	/// ファイルパスからログカテゴリを判定する
	/// </summary>
	private static LogCategory DetermineCategory(string filePath) {
		if (string.IsNullOrEmpty(filePath)) return LogCategory.Application;
		
		// Scripts/Engine (または Scripts\Engine) フォルダ内のファイルなら ScriptEngine カテゴリ
		if (filePath.Contains("Scripts/Engine") || filePath.Contains("Scripts\\Engine")) {
			return LogCategory.ScriptEngine;
		}
		
		return LogCategory.Application;
	}

	/// <summary>
	/// 明示的にカテゴリを指定して出力する場合（内部用）
	/// </summary>
	static internal void InternalLog(string message, LogCategory category = LogCategory.ScriptEngine) {
#if DEBUG
		InternalConsoleLog(message, category);
#endif
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalConsoleLog(string s, LogCategory category);
}