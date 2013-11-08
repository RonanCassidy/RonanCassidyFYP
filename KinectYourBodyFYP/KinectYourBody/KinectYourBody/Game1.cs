using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using Microsoft.Kinect;


namespace KinectYourBody
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        KinectSensor kinect;
        Texture2D colorVideo, depthVideo, jointTexture, starTexture, faceTexture, handTexture;

        Skeleton[] skeletonData;
        Skeleton skeleton;

        int leftHits;
        int rightHits;
        bool hit;
        SpriteFont Font1;
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            kinect = KinectSensor.KinectSensors[0];
            kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinect.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
           
            
            kinect.SkeletonStream.Enable();
            kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(kinect_AllFramesReady);

            kinect.Start();
            colorVideo = new Texture2D(graphics.GraphicsDevice, kinect.ColorStream.FrameWidth, kinect.ColorStream.FrameHeight);
            depthVideo = new Texture2D(graphics.GraphicsDevice, kinect.DepthStream.FrameWidth, kinect.DepthStream.FrameHeight);
            base.Initialize();
        }

        
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            //kinect.ElevationAngle = 20;

            kinect.ElevationAngle = 20;

            jointTexture = Content.Load<Texture2D>("joint");
            starTexture = Content.Load<Texture2D>("star");
            faceTexture = Content.Load<Texture2D>("face");
            handTexture = Content.Load<Texture2D>("hand");
            Font1 = Content.Load<SpriteFont>("font1");
            hit = false;
            leftHits = 0;
            rightHits = 0;
        }

        
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.Draw(colorVideo, new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight), Color.White);
            //spriteBatch.Draw(depthVideo, new Rectangle(640, 0, colorVideo.Width, colorVideo.Height), Color.White);
            DrawSkeleton(spriteBatch, new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
            CheckStuff(spriteBatch);
           // spriteBatch.Draw(starTexture, new Rectangle(50,200,64,64),Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
        void kinect_AllFramesReady(object sender, AllFramesReadyEventArgs imageFrames)
        {
            //
            //Color Frame
            //
            ColorImageFrame colorVideoFrame = imageFrames.OpenColorImageFrame();

            if (colorVideoFrame != null)
            {
                //Create array for pixel data and copy it from the image frame
                Byte[] pixelData = new Byte[colorVideoFrame.PixelDataLength];
                colorVideoFrame.CopyPixelDataTo(pixelData);

                //Convert RGBA to BGRA
                Byte[] bgraPixelData = new Byte[colorVideoFrame.PixelDataLength];
                for (int i = 0; i < pixelData.Length; i += 4)
                {
                    bgraPixelData[i] = pixelData[i + 2];
                    bgraPixelData[i + 1] = pixelData[i + 1];
                    bgraPixelData[i + 2] = pixelData[i];
                    bgraPixelData[i + 3] = (Byte)255; //The video comes with 0 alpha so it is transparent
                }

                // Create a texture and assign the realigned pixels
                colorVideo = new Texture2D(graphics.GraphicsDevice, colorVideoFrame.Width, colorVideoFrame.Height);
                colorVideo.SetData(bgraPixelData);
            }

            //
            // Skeleton Frame
            //
            using (SkeletonFrame skeletonFrame = imageFrames.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    if ((skeletonData == null) || (this.skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    //Copy the skeleton data to our array
                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);
                }
             }
            if (skeletonData != null)
            {
                foreach (Skeleton skel in skeletonData)
                {
                    if (skel.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        skeleton = skel;
                    }
                }
            }
        
        
            DepthImageFrame depthVideoFrame = imageFrames.OpenDepthImageFrame();

            //short[] pixelData = new short[depthVideoFrame.PixelDataLength];
             if (depthVideoFrame != null)
              {
                short[] pixelData = new short[depthVideoFrame.PixelDataLength];
                depthVideoFrame.CopyPixelDataTo(pixelData);
                depthVideo = new Texture2D(graphics.GraphicsDevice, depthVideoFrame.Width, depthVideoFrame.Height);
                depthVideo.SetData(ConvertDepthFrame(pixelData, kinect.DepthStream));
              }
        }
        private void CheckStuff(SpriteBatch spriteBatch)
        {

            if (skeleton != null)
            {
                spriteBatch.DrawString(Font1, "Hits With Right", new Vector2(10, 10), Color.White);
                spriteBatch.DrawString(Font1, rightHits.ToString(), new Vector2(200, 10), Color.White);


                spriteBatch.DrawString(Font1, "Hits With Left", new Vector2(10, 50), Color.White);
                spriteBatch.DrawString(Font1, leftHits.ToString(), new Vector2(200, 50), Color.White);
             
                foreach (Joint joint in skeleton.Joints)
                {
                    if (skeleton.Joints[JointType.KneeRight].Position.Y > skeleton.Joints[JointType.KneeLeft].Position.Y )
                    {
                        spriteBatch.Draw(starTexture, new Rectangle(50, 200, 64, 64), Color.White);
                    }
                    if (skeleton.Joints[JointType.Head].Position.Y < skeleton.Joints[JointType.HandRight].Position.Y )
                    {
                        spriteBatch.Draw(starTexture, new Rectangle(50, 300, 64, 64), Color.White);
                    }

                    //////////BOXING//////////////
                    //////////Right//////////////
                    if (skeleton.Joints[JointType.HandRight].Position.Z > skeleton.Joints[JointType.HandLeft].Position.Z && hit == false )
                    {
                        rightHits++;
                        spriteBatch.Draw(starTexture, new Rectangle(50, 300, 64, 64), Color.White);
                        hit = true;
                    }
                    //////////Left/////////////
                    else if (skeleton.Joints[JointType.HandRight].Position.Z < skeleton.Joints[JointType.HandLeft].Position.Z && hit == true)
                    {
                        leftHits++;
                        spriteBatch.Draw(starTexture, new Rectangle(1000, 300, 64, 64), Color.White);
                        hit = false;
                    }


                }
            }
        }
        private void DrawSkeleton(SpriteBatch spriteBatch, Vector2 resolution)
        {
            if (skeleton != null)
            {
                foreach (Joint joint in skeleton.Joints)
                {
                    if(joint == skeleton.Joints[JointType.Head])
                    {
                        Vector2 position = new Vector2((((0.5f * joint.Position.X) + 0.5f) * (resolution.X)) - 32, (((-0.5f * joint.Position.Y) + 0.5f) * (resolution.Y)) - 32);
                        spriteBatch.Draw(faceTexture, new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), 64, 64), Color.White);
                    }
                    if (joint == skeleton.Joints[JointType.HandRight] || joint == skeleton.Joints[JointType.HandLeft])
                    {
                        Vector2 position = new Vector2((((0.5f * joint.Position.X) + 0.5f) * (resolution.X))-64, (((-0.5f * joint.Position.Y) + 0.5f) * (resolution.Y))-64);
                        spriteBatch.Draw(handTexture, new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), 128, 128), Color.White);
                    }
                    if (joint != skeleton.Joints[JointType.HandRight] && joint != skeleton.Joints[JointType.HandLeft] && joint != skeleton.Joints[JointType.Head])
                    {
                        Vector2 position = new Vector2((((0.5f * joint.Position.X) + 0.5f) * (resolution.X)), (((-0.5f * joint.Position.Y) + 0.5f) * (resolution.Y)));
                        spriteBatch.Draw(jointTexture, new Rectangle(Convert.ToInt32(position.X), Convert.ToInt32(position.Y), 10, 10), Color.Red);
               
                    }
                }
            }
        }
        private byte[] ConvertDepthFrame(short[] depthFrame, DepthImageStream depthStream)
        {
            int RedIndex = 0, GreenIndex = 1, BlueIndex = 2, AlphaIndex = 3;

            byte[] depthFrame32 = new byte[depthStream.FrameWidth * depthStream.FrameHeight * 4];

            for (int i16 = 0, i32 = 0; i16 < depthFrame.Length && i32 < depthFrame32.Length; i16++, i32 += 4)
            {
                int player = depthFrame[i16] & DepthImageFrame.PlayerIndexBitmask;
                int realDepth = depthFrame[i16] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                // transform 13-bit depth information into an 8-bit intensity appropriate
                // for display (we disregard information in most significant bit)
                byte intensity = (byte)(~(realDepth >> 4));

                depthFrame32[i32 + RedIndex] = (byte)(intensity);
                depthFrame32[i32 + GreenIndex] = (byte)(intensity);
                depthFrame32[i32 + BlueIndex] = (byte)(intensity);
                depthFrame32[i32 + AlphaIndex] = 255;
            }
            return depthFrame32;
        }
    }
}

