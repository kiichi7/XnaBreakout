// Project: XnaBreakout, File: FileHelper.cs
// Namespace: XnaBreakout.Helpers, Class: FileHelper
// Path: C:\code\XnaBreakout\Helpers, Author: Abi
// Code lines: 137, Size of file: 4,13 KB
// Creation date: 15.10.2006 09:08
// Last modified: 22.10.2006 18:03
// Generated with Commenter by abi.exDream.com

#region Using directives
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
#endregion

namespace XnaBreakout.Helpers
{
	/// <summary>
	/// File helper class to get text lines, number of text lines, etc.
	/// Update: Now also supports the XNA Storage classes :)
	/// </summary>
	public class FileHelper
	{
		#region LoadGameContentFile
		/// <summary>
		/// Load game content file, returns null if file was not found.
		/// </summary>
		/// <param name="relativeFilename">Relative filename</param>
		/// <returns>File stream</returns>
		public static FileStream LoadGameContentFile(string relativeFilename)
		{
			string fullPath = Path.Combine(
				StorageContainer.TitleLocation, relativeFilename);
			if (File.Exists(fullPath) == false)
				return null;
			else
				return File.Open(fullPath,
					FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		} // LoadGameContentFile(relativeFilename)
		#endregion

		#region Get text lines
		/// <summary>
		/// Returns the number of text lines we got in a file.
		/// </summary>
		static public string[] GetLines(string filename)
		{
			try
			{
				StreamReader reader = new StreamReader(
					new FileStream(filename, FileMode.Open, FileAccess.Read),
					System.Text.Encoding.UTF8);
				// Generic version
				List<string> lines = new List<string>();
				do
				{
					lines.Add(reader.ReadLine());
				} while (reader.Peek() > -1);
				reader.Close();
				return lines.ToArray();
			} // try
			catch
			{
				// Failed to read, just return null!
				return null;
			} // catch
		} // GetLines(filename)
		#endregion

		#region Create text file
		/// <summary>
		/// Create text file
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="textForFile">Text for file</param>
		/// <exception cref="IOException">
		/// Will be thrown if file already exists.
		/// </exception>
		public static void CreateTextFile(
			string filename, string textForFile,
			Encoding fileEncoding)
		{
			StreamWriter textWriter = new StreamWriter(
				new FileStream(filename, FileMode.Create, FileAccess.Write),
				fileEncoding);//System.Text.Encoding.UTF8);

			string[] textLines = StringHelper.SplitMultilineText(textForFile);
			foreach (string line in textLines)
				textWriter.WriteLine(line);
			textWriter.Close();
		} // CreateTextFile(filename, textForFile)
		#endregion
	}	// class FileHelper
} // namespace XnaBreakout.Helpers
