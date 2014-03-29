// Author: Ben
// Project: XnaBreakout
// Path: C:\code\Xna\XnaBreakout
// Creation date: 24.11.2005 12:46
// Last modified: 27.01.2008 15:51

#region Using directives
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using XnaBreakout.Helpers;
#endregion

namespace XnaBreakout
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	partial class BreakoutGame : Microsoft.Xna.Framework.Game
	{
		#region Constants
		/// <summary>
		/// Rectangles for our graphics, tested with the unit tests below.
		/// </summary>
		static readonly Rectangle
			GamePaddleRect = new Rectangle(39, 1, 93, 23),
			GameBallRect = new Rectangle(1, 1, 36, 36),
			GameBlockRect = new Rectangle(136, 1, 62, 27),
			GameYouWonRect = new Rectangle(2, 39, 92, 21),
			GameYouLostRect = new Rectangle(105, 39, 94, 21);

		/// <summary>
		/// Ball speed multiplicator, this is how much screen space the ball will
		/// travel each second.
		/// </summary>
		const float BallSpeedMultiplicator = 0.85f;//1;//0.75f;//0.5f;1

		/// <summary>
		/// How many block columns and rows are displayed?
		/// </summary>
		const int NumOfColumns = 14,
			NumOfRows = 12;
		#endregion

		#region Variables
		/// <summary>
		/// Graphics
		/// </summary>
		GraphicsDeviceManager graphics;
		/// <summary>
		/// Background texture
		/// </summary>
		Texture2D backgroundTexture, gameTexture;
		/// <summary>
		/// Audio engine
		/// </summary>
		AudioEngine audioEngine;
		/// <summary>
		/// Wave bank
		/// </summary>
		WaveBank waveBank;
		/// <summary>
		/// Sound bank
		/// </summary>
		SoundBank soundBank;

		/// <summary>
		/// Resolution of our game.
		/// </summary>
		int width, height;

		/// <summary>
		/// Current paddle positions, 0 means left, 1 means right.
		/// </summary>
		float paddlePosition = 0.5f;

		/// <summary>
		/// Current ball position.
		/// </summary>
		Vector2 ballPosition = new Vector2(0.5f, 0.95f - 0.035f);
		/// <summary>
		/// Ball speed vector.
		/// </summary>
		Vector2 ballSpeedVector = new Vector2(0, 0);

		/// <summary>
		/// Paddle, ball, block, etc. as sprite helpers.
		/// </summary>
		SpriteHelper paddle, ball, block, youWon, youLost, background;

		/// <summary>
		/// <returns>-</returns>
		/// Level we are in and the current score.
		/// Just updated in the windows title because we don't have
		/// font support yet.
		/// </summary>
		int level = 0, score = -1;
		/// <summary>
		/// Wait until user presses space or A to start a level.
		/// </summary>
		/// <returns>False</returns>
		bool pressSpaceToStart = true, lostGame = false;

		/// <summary>
		/// All blocks of the current play field. If they are
		/// all cleared, we advance to the next level.
		/// </summary>
		bool[,] blocks = new bool[NumOfColumns, NumOfRows];

		/// <summary>
		/// Block positions for each block we have, initialized in Initialize().
		/// </summary>
		Vector2[,] blockPositions = new Vector2[NumOfColumns, NumOfRows];

		/// <summary>
		/// Bounding boxes for each of the blocks, also precalculated and checked
		/// each frame if the ball collides with one of the blocks.
		/// </summary>
		BoundingBox[,] blockBoxes = new BoundingBox[NumOfColumns, NumOfRows];
		#endregion

		#region Constructor
		/// <summary>
		/// Create the breakout game
		/// </summary>
		public BreakoutGame()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";

			// Don't limit the framerate to the vertical retrace
			graphics.SynchronizeWithVerticalRetrace = false;
			this.IsFixedTimeStep = false;
		} // BreakoutGame()

		/// <summary>
		/// Initialize our game and load all graphics.
		/// </summary>
		protected override void Initialize()
		{
			// Remember resolution
			width = graphics.GraphicsDevice.Viewport.Width;
			height = graphics.GraphicsDevice.Viewport.Height;

			// Init all blocks, set positions and bounding boxes
			for (int y = 0; y < NumOfRows; y++)
				for (int x = 0; x < NumOfColumns; x++)
				{
					blockPositions[x, y] = new Vector2(
						0.05f + 0.9f * x / (float)(NumOfColumns - 1),
						0.066f + 0.5f * y / (float)(NumOfRows - 1));
					Vector3 pos = new Vector3(blockPositions[x, y], 0);
					Vector3 blockSize = new Vector3(
						GameBlockRect.X / 1024.0f, GameBlockRect.Y / 768, 0);
					blockBoxes[x, y] = new BoundingBox(
						pos - blockSize / 2,
						pos + blockSize / 2);
				} // for for (int)

			// Start with level 1
			StartLevel();

			base.Initialize();
		} // Initialize()

		/// <summary>
		/// Load all graphics content (just our background texture).
		/// Use this method to make sure a device reset event is handled correctly.
		/// </summary>
		protected override void LoadContent()
		{
			// Load all our content
			backgroundTexture = Content.Load<Texture2D>("SpaceBackground");
			// Game texture
			gameTexture = Content.Load<Texture2D>("BreakoutGame");
			audioEngine = new AudioEngine("Content\\BreakoutSound.xgs");
			waveBank = new WaveBank(audioEngine, "Content\\Wave Bank.xwb");
			if (waveBank != null)
				soundBank = new SoundBank(audioEngine, "Content\\Sound Bank.xsb");

			// Create all sprites
			paddle = new SpriteHelper(gameTexture, GamePaddleRect);
			ball = new SpriteHelper(gameTexture, GameBallRect);
			block = new SpriteHelper(gameTexture, GameBlockRect);
			// You won sound
			youWon = new SpriteHelper(gameTexture, GameYouWonRect);
			// You lost sound
			youLost = new SpriteHelper(gameTexture, GameYouLostRect);
			// And finally the background texture as a sprite
			background = new SpriteHelper(backgroundTexture, null);
		} // LoadContent()

		/// <summary>
		/// Unload graphic content if the device gets lost.
		/// </summary>
		protected override void UnloadContent()
		{
			Content.Unload();
			SpriteHelper.Dispose();
		} // UnloadContent()
		#endregion

		#region Start level
		/// <summary>
		/// Start level
		/// </summary>
		void StartLevel()
		{
			// Randomize levels, but make it more harder each level
			for (int y = 0; y < NumOfRows; y++)
				for (int x = 0; x < NumOfColumns; x++)
					blocks[x, y] = RandomHelper.GetRandomInt(10) < level + 1;
			// Use the lower blocks only for later levels
			if (level < 6)
				for (int x = 0; x < NumOfColumns; x++)
					blocks[x, NumOfRows - 1] = false;
			if (level < 4)
				for (int x = 0; x < NumOfColumns; x++)
					blocks[x, NumOfRows - 2] = false;
			if (level < 2)
				for (int x = 0; x < NumOfColumns; x++)
					blocks[x, NumOfRows - 3] = false;

			// Halt game
			ballSpeedVector = Vector2.Zero;

			// Wait until user presses space or A to start a level.
			pressSpaceToStart = true;

			// Update title
			Window.Title =
				"XnaBreakout - Level " + (level + 1) + " - Score " + Math.Max(0, score);
		} // StartLevel()
		#endregion

		#region Start and stop ball
		/// <summary>
		/// Start new ball at the beginning of each game and when a ball is lost.
		/// </summary>
		public void StartNewBall()
		{
			// Randomize direction, but always go up
			ballSpeedVector =
				new Vector2(RandomHelper.GetRandomFloat(-1, +1), -1);
			ballSpeedVector.Normalize();
			// Make sure game is started now
			if (score < 0)
				score = 0;
			// If we lost the game and restarted, reset score!
			if (lostGame)
			{
				// Game over, reset to level 0
				level = 0;
				score = 0;
				lostGame = false;
				StartLevel();
			} // if (lostGame)
			// Clear message
			pressSpaceToStart = false;
		} // StartNewBall()

		/// <summary>
		/// Stop ball for menu and when game is over.
		/// </summary>
		public void StopBall()
		{
			ballSpeedVector = new Vector2(0, 0);
			pressSpaceToStart = true;
		} // StopBall()
		#endregion

		#region Update
		/// <summary>
		/// Game pad
		/// </summary>
		GamePadState gamePad;
		/// <summary>
		/// Keyboard
		/// </summary>
		KeyboardState keyboard;
		/// <summary>
		/// Update game input.
		/// </summary>
		protected override void Update(GameTime gameTime)
		{
			// The time since Update was called last
			float elapsed =
				(float)gameTime.ElapsedGameTime.TotalSeconds;

			// Get keyboard and gamepad states
			keyboard = Keyboard.GetState();
			gamePad = GamePad.GetState(PlayerIndex.One);

			// Escape and back exit the game
			if (keyboard.IsKeyDown(Keys.Escape) ||
				gamePad.Buttons.Back == ButtonState.Pressed)
				this.Exit();

			// Move half way across the screen each second
			float moveFactorPerSecond = 0.75f *
				(float)gameTime.ElapsedRealTime.TotalMilliseconds / 1000.0f;

			// Move left and right if we press the cursor or gamepad keys.
			if (gamePad.DPad.Right == ButtonState.Pressed ||
				gamePad.ThumbSticks.Left.X > 0.5f ||
				keyboard.IsKeyDown(Keys.Right))
				paddlePosition += moveFactorPerSecond;
			if (gamePad.DPad.Left == ButtonState.Pressed ||
				gamePad.ThumbSticks.Left.X < -0.5f ||
				keyboard.IsKeyDown(Keys.Left))
				paddlePosition -= moveFactorPerSecond;

			// Make sure paddle stay between 0 and 1 (offset 0.05f for paddle width)
			if (paddlePosition < 0.05f)
				paddlePosition = 0.05f;
			if (paddlePosition > 1 - 0.05f)
				paddlePosition = 1 - 0.05f;

			// Game not started yet? Then put ball on paddle.
			if (pressSpaceToStart)
			{
				ballPosition = new Vector2(paddlePosition, 0.95f - 0.035f);

				// Handle space
				if (keyboard.IsKeyDown(Keys.Space) ||
					gamePad.Buttons.A == ButtonState.Pressed)
				{
					StartNewBall();
				} // if (keyboard.IsKeyDown)
			} // if (pressSpaceToStart)
			else
			{
				// Check collisions
				CheckBallCollisions(moveFactorPerSecond);

				// Update ball position and bounce off the borders
				ballPosition += ballSpeedVector *
					moveFactorPerSecond * BallSpeedMultiplicator;

				// Ball lost?
				if (ballPosition.Y > 0.985f)
				{
					// Play sound
					soundBank.PlayCue("PongBallLost");
					// Show lost message, reset is done above in StartNewBall!
					lostGame = true;
					pressSpaceToStart = true;
				} // if (ballPosition.Y)

				// Check if all blocks are killed and if we won this level
				bool allBlocksKilled = true;
				for (int y = 0; y < NumOfRows; y++)
					for (int x = 0; x < NumOfColumns; x++)
						if (blocks[x, y])
						{
							allBlocksKilled = false;
							break;
						} // for for if (blocks[x,)

				// We won, start next level
				if (allBlocksKilled == true)
				{
					// Play sound
					soundBank.PlayCue("BreakoutVictory");
					lostGame = false;
					level++;
					StartLevel();
				} // if (allBlocksKilled)
			} // else

			base.Update(gameTime);
		} // Update(gameTime)
		#endregion

		#region Check ball collisions
		/// <summary>
		/// Check ball collisions
		/// </summary>
		void CheckBallCollisions(float moveFactorPerSecond)
		{
			// Check top, left and right screen borders
			float MinYPos = 0.0235f;
			if (ballPosition.Y < MinYPos)
			{
				ballSpeedVector.Y = -ballSpeedVector.Y;
				// Move ball back into screen space
				if (ballPosition.X < MinYPos)
					ballPosition.X = MinYPos;
				// Play hit sound
				soundBank.PlayCue("PongBallHit");
			} // if (ballPosition.Y)
			float MinXPos = 0.018f;
			if (ballPosition.X < MinXPos ||
				ballPosition.X > 1 - MinXPos)
			{
				ballSpeedVector.X = -ballSpeedVector.X;
				// Move ball back into screen space
				if (ballPosition.X < MinXPos)
					ballPosition.X = MinXPos;
				if (ballPosition.X > 1 - MinXPos)
					ballPosition.X = 1 - MinXPos;
				// Play hit sound
				soundBank.PlayCue("PongBallHit");
			} // if (ballPosition.X)

			// Check for collisions with the paddles
			// Construct bounding boxes to use the intersection helper method.
			Vector2 ballSize = new Vector2(24 / 1024.0f, 24 / 768.0f);
			BoundingBox ballBox = new BoundingBox(
				new Vector3(ballPosition.X - ballSize.X / 2, ballPosition.Y - ballSize.Y / 2, 0),
				new Vector3(ballPosition.X + ballSize.X / 2, ballPosition.Y + ballSize.Y / 2, 0));
			Vector2 paddleSize = new Vector2(
				GamePaddleRect.Width / 1024.0f, GamePaddleRect.Height / 768.0f);
			BoundingBox paddleBox = new BoundingBox(
				new Vector3(paddlePosition - paddleSize.X / 2, 0.95f - paddleSize.Y * 0.7f, 0),
				new Vector3(paddlePosition + paddleSize.X / 2, 0.95f, 0));

			// Ball hit paddle?
			if (ballBox.Intersects(paddleBox))
			{
				// Bounce off in the direction vector from the paddle
				ballSpeedVector.X += (ballPosition.X - paddlePosition) / (MinXPos * 3);
				// Max to -1 and +1
				if (ballSpeedVector.X < -1)
					ballSpeedVector.X = -1;
				if (ballSpeedVector.X > 1)
					ballSpeedVector.X = 1;
				// Bounce of the paddle
				ballSpeedVector.Y = -1;// -ballSpeedVector.Y;
				// Move away from the paddle
				ballPosition.Y -= moveFactorPerSecond * BallSpeedMultiplicator;
				// Normalize vector
				ballSpeedVector.Normalize();
				// Play sound
				soundBank.PlayCue("PongBallHit");
			} // if (ballBox.Intersects)

			// Ball hits any block?
			for (int y = 0; y < NumOfRows; y++)
				for (int x = 0; x < NumOfColumns; x++)
					if (blocks[x, y])
					{
						// Collision check
						if (ballBox.Intersects(blockBoxes[x, y]))
						{
							// Kill block
							blocks[x, y] = false;
							// Add score
							score++;
							// Update title
							Window.Title =
								"XnaBreakout - Level " + (level + 1) + " - Score " + score;
							// Play sound
							soundBank.PlayCue("BreakoutBlockKill");
							// Bounce ball back, but first find out which side we hit.
							// Start with left/right borders.
							if (Math.Abs(blockBoxes[x, y].Max.X - ballBox.Min.X) <
								moveFactorPerSecond)
							{
								ballSpeedVector.X = Math.Abs(ballSpeedVector.X);
								// Also move back a little
								ballPosition.X += (ballSpeedVector.X < 0 ? -1 : 1) *
									moveFactorPerSecond;
							} // if (Math.Abs)
							else if (Math.Abs(blockBoxes[x, y].Min.X - ballBox.Max.X) <
								moveFactorPerSecond)
							{
								ballSpeedVector.X = -Math.Abs(ballSpeedVector.X);
								// Also move back a little
								ballPosition.X += (ballSpeedVector.X < 0 ? -1 : 1) *
									moveFactorPerSecond;
							} // else if
							// Now check top/bottom borders
							else if (Math.Abs(blockBoxes[x, y].Max.Y - ballBox.Min.Y) <
								moveFactorPerSecond)
							{
								ballSpeedVector.Y = Math.Abs(ballSpeedVector.Y);
								// Also move back a little
								ballPosition.Y += (ballSpeedVector.Y < 0 ? -1 : 1) *
									moveFactorPerSecond;
							} // else if
							else if (Math.Abs(blockBoxes[x, y].Min.Y - ballBox.Max.Y) <
								moveFactorPerSecond)
							{
								ballSpeedVector.Y = -Math.Abs(ballSpeedVector.Y);
								// Also move back a little
								ballPosition.Y += (ballSpeedVector.Y < 0 ? -1 : 1) *
									moveFactorPerSecond;
							} // else if
							else
								ballSpeedVector *= -1;

							// Go outa here, only handle 1 block at a time
							break;
						} // if (ballBox.Intersects)
					} // for for if (blocks[x,)
		} // CheckBallCollisions(moveFactorPerSecond)
		#endregion

		#region Draw
		/// <summary>
		/// Draws all sprites and game components.
		/// </summary>
		protected override void Draw(GameTime gameTime)
		{
			// Render background
			background.Render();
			SpriteHelper.DrawSprites(width, height);

			// Render all game graphics			
			paddle.RenderCentered(paddlePosition, 0.95f);
			ball.RenderCentered(ballPosition);
			// Render all blocks
			for (int y = 0; y < NumOfRows; y++)
				for (int x = 0; x < NumOfColumns; x++)
					if (blocks[x, y])
						block.RenderCentered(blockPositions[x, y]);

			if (pressSpaceToStart &&
				score >= 0)
			{
				if (lostGame)
					youLost.RenderCentered(0.5f, 0.65f, 2);
				else
					youWon.RenderCentered(0.5f, 0.65f, 2);
			} // if (pressSpaceToStart)

			// Draw all sprites on the screen
			SpriteHelper.DrawSprites(width, height);

			base.Draw(gameTime);
		} // Draw(gameTime)
		#endregion

		#region Start game
		/// <summary>
		/// Start game
		/// </summary>
		public static void StartGame()
		{
			using (BreakoutGame game = new BreakoutGame())
			{
				game.Run();
			} // using (game)
		} // StartGame()
		#endregion

		#region Unit tests
#if DEBUG
		#region Test delegate
		/// <summary>
		/// Test delegate helper
		/// </summary>
		delegate void TestDelegate();
		#endregion

		#region TestPongGame class
		/// <summary>
		/// Helper class to test the BreakoutGame
		/// </summary>
		/// <param name="setTestLoop">Set test loop</param>
		class TestBreakoutGame : BreakoutGame
		{
			/// <summary>
			/// Test loop
			/// </summary>
			TestDelegate testLoop;
			public TestBreakoutGame(TestDelegate setTestLoop)
			{
				testLoop = setTestLoop;
			} // TestBreakoutGame(setTestLoop)

			/// <summary>
			/// Draw
			/// </summary>
			/// <param name="gameTime">Game time</param>
			protected override void Draw(GameTime gameTime)
			{
				base.Draw(gameTime);
				testLoop();
			} // Draw(gameTime)
		} // class TestBreakoutGame
		#endregion

		#region StartTest
		/// <summary>
		/// Test game
		/// </summary>
		static TestBreakoutGame testGame;
		/// <summary>
		/// Start test
		/// </summary>
		/// <param name="testLoop">Test loop</param>
		static void StartTest(TestDelegate testLoop)
		{
			testGame = new TestBreakoutGame(testLoop);
			testGame.Run();
			testGame.Dispose();
		} // StartTest(testLoop)
		#endregion

		#region TestSounds
		/// <summary>
		/// Test sounds
		/// </summary>
		public static void TestSounds()
		{
			StartTest(
				delegate
				{
					string soundToPlay = "";
					if (testGame.keyboard.IsKeyDown(Keys.Space))
						soundToPlay = "PongBallHit";
					if (testGame.keyboard.IsKeyDown(Keys.LeftControl))
						soundToPlay = "PongBallLost";
					if (testGame.keyboard.IsKeyDown(Keys.LeftAlt))
						soundToPlay = "BreakoutVictory";
					if (testGame.keyboard.IsKeyDown(Keys.LeftShift))
						soundToPlay = "BreakoutBlockKill";

					if (String.IsNullOrEmpty(soundToPlay) == false)
					{
						testGame.soundBank.PlayCue(soundToPlay);
						System.Threading.Thread.Sleep(500);
					} // if
				});
		} // TestSounds()
		#endregion

		#region TestGameSprites
		/// <summary>
		/// Test game sprites
		/// </summary>
		public static void TestGameSprites()
		{
			StartTest(
				delegate
				{
					if (testGame.keyboard.IsKeyDown(Keys.LeftControl))
						testGame.youLost.RenderCentered(0.5f, 0.65f, 2);
					else
						testGame.youWon.RenderCentered(0.5f, 0.65f, 2);
				});
		} // TestGameSprites()
		#endregion

		#region TestBallCollisions
		/// <summary>
		/// Test ball collisions
		/// </summary>
		public static void TestBallCollisions()
		{
			StartTest(
				delegate
				{
					testGame.Window.Title = "XnaBreakout - Press 1-5 to start collision tests";

					// Start specific collision scene based on the user input.
					if (testGame.keyboard.IsKeyDown(Keys.D1))
					{
						// First test, just collide with screen border
						testGame.ballPosition = new Vector2(0.9f, 0.5f);
						testGame.ballSpeedVector = new Vector2(1, 1);
						testGame.ballSpeedVector.Normalize();
						testGame.pressSpaceToStart = false;
					} // if
					else if (testGame.keyboard.IsKeyDown(Keys.D2))
					{
						// Second test, straight on collision with paddle
						testGame.ballPosition = new Vector2(0.6f, 0.8f);
						testGame.ballSpeedVector = new Vector2(1, 1);
						testGame.ballSpeedVector.Normalize();
						testGame.paddlePosition = 0.7f;
						testGame.pressSpaceToStart = false;
					} // if
					else if (testGame.keyboard.IsKeyDown(Keys.D3))
					{
						// Advanced test to check if we hit the edge of paddle
						testGame.ballPosition = new Vector2(0.9f, 0.4f);
						testGame.ballSpeedVector = new Vector2(1, -0.5f);
						testGame.ballSpeedVector.Normalize();
						testGame.paddlePosition = 0.29f;
						testGame.pressSpaceToStart = false;
					} // if
					else if (testGame.keyboard.IsKeyDown(Keys.D4))
					{
						// Advanced test to check if we hit the edge of paddle
						testGame.ballPosition = new Vector2(0.9f, 0.4f);
						testGame.ballSpeedVector = new Vector2(1, -0.5f);
						testGame.ballSpeedVector.Normalize();
						testGame.paddlePosition = 0.42f;
						testGame.pressSpaceToStart = false;
					} // if
					else if (testGame.keyboard.IsKeyDown(Keys.D4))
					{
						// And finally test collision with blocks of current level
						testGame.StartLevel();
						testGame.pressSpaceToStart = false;
						testGame.ballPosition = new Vector2(0.9f, 0.4f);
						testGame.ballSpeedVector = new Vector2(1, -0.5f);
						testGame.ballSpeedVector.Normalize();
					} // if
				});
		} // TestBallCollisions()
		#endregion
#endif
		#endregion
	} // class BreakoutGame
} // namespace XnaBreakout
