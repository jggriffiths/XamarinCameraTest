using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Drawing;
using Android.Util;
using Android.Graphics;

//using GL1 = OpenTK.Graphics.ES11.GL;
//using All1 = OpenTK.Graphics.ES11.All;
using OpenTK.Platform;
using OpenTK.Platform.Android;
using System.Collections.Generic;
using Java.Interop;
using Android.Content.Res;
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
using Javax.Microedition.Khronos.Egl;
using Java.Nio;
using Android.Hardware;

namespace XamarinCameraTest
{
    [Activity(Label = "XamarinCameraTest", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        int count = 1;
        private PowerManager.WakeLock mWL;
        private MainView mView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // full screen & full brightness
            this.RequestWindowFeature(WindowFeatures.NoTitle);
            Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
            mWL = ((PowerManager)GetSystemService(Context.PowerService)).NewWakeLock(WakeLockFlags.Full, "BOB");
            mWL.Acquire();
            mView = new MainView(this);
            SetContentView(mView);
            LinearLayout ll = new LinearLayout(this);
            Button b = new Button(this);
            b.SetText("Hello", TextView.BufferType.Normal);
            ll.AddView(b);
            ll.SetGravity(GravityFlags.CenterHorizontal | GravityFlags.CenterVertical);
            this.AddContentView(ll,
                new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent));
        }
    }

    // View
    public class MainView : GLSurfaceView
    {
        MainRenderer mRenderer;

        public MainView(Context context) : base(context)
        {
            mRenderer = new MainRenderer(this);
            SetEGLContextClientVersion(2);
            SetRenderer(mRenderer);
            this.RenderMode = Android.Opengl.Rendermode.WhenDirty;
        }

        public void surfaceCreated(ISurfaceHolder holder)
        {
            base.SurfaceCreated(holder);
        }

        public void surfaceDestroyed(ISurfaceHolder holder)
        {
            mRenderer.Dispose();
            base.SurfaceDestroyed(holder);
        }

        public void surfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int w, int h)
        {
            base.SurfaceChanged(holder, format, w, h);
        }
    }
}


