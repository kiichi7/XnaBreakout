// Author: Ben
// Project: XnaBreakout
// Path: C:\code\Xna\XnaBreakout
// Creation date: 27.01.2008 14:55
// Last modified: 27.01.2008 15:38

#region Using directives
using System;
#endregion

namespace XnaBreakout
{
	/// <summary>
	/// Program
	/// </summary>
	static class Program
	{
		#region Main
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			BreakoutGame.StartGame();

			// Static unit tests:
			//BreakoutGame.TestGameSprites();
			//BreakoutGame.TestSounds();
			//BreakoutGame.TestBallCollisions();
		} // Main(args)
		#endregion
	} // class Program
} // namespace XnaBreakout
