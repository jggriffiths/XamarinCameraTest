using System;
using Java.Nio;

/*using GL1 = OpenTK.Graphics.ES11.GL;
using All1 = OpenTK.Graphics.ES11.All;
using GL2 = OpenTK.Graphics.ES20.GL;*/
using Android.Hardware;
using Android.Graphics;
using Android.Opengl;

namespace XamarinCameraTest
{
    public class MainRenderer : Java.Lang.Object, GLSurfaceView.IRenderer, SurfaceTexture.IOnFrameAvailableListener
    {

        private const string TAG = "MyGLRenderer";
        private Triangle mTriangle;

        private const string vss =
            "attribute vec2 vPosition;\n" +
            "attribute vec2 vTexCoord;\n" +
            "varying vec2 texCoord;\n" +
            "void main() {\n" +
            "  texCoord = vTexCoord;\n" +
            "  gl_Position = vec4 ( vPosition.x, vPosition.y, 0.0, 1.0 );\n" +
            "}";

        private const string fss =
            "#extension GL_OES_EGL_image_external : require\n" +
            "precision mediump float;\n" +
            "uniform samplerExternalOES sTexture;\n" +
            "varying vec2 texCoord;\n" +
            "void main() {\n" +
            "  gl_FragColor = texture2D(sTexture,texCoord);\n" +
            "}";


        private float[] mMVPMatrix = new float[16];
        private float[] mProjectionMatrix = new float[16];
        private float[] mViewMatrix = new float[16];
        private float[] mRotationMatrix = new float[16];

        private int[] hTex;
        private FloatBuffer pVertex;
        private FloatBuffer pTexCoord;
        private bool mUpdateST = false;
        private MainView mView;
        private int hProgram;
        private Android.Hardware.Camera mCamera;
        private SurfaceTexture mSTexture;

        private float mAngle;

        public MainRenderer(MainView view)
        {
            mView = view;
            float[] vtmp = { 1.0f, -1.0f, -1.0f, -1.0f, 1.0f, 1.0f, -1.0f, 1.0f };
            float[] ttmp = { 1.0f, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f };
            pVertex = ByteBuffer.AllocateDirect(8 * 4).Order(ByteOrder.NativeOrder()).AsFloatBuffer();
            pVertex.Put(vtmp);
            pVertex.Position(0);
            pTexCoord = ByteBuffer.AllocateDirect(8 * 4).Order(ByteOrder.NativeOrder()).AsFloatBuffer();
            pTexCoord.Put(ttmp);
            pTexCoord.Position(0);
        }

        public void OnDrawFrame(Javax.Microedition.Khronos.Opengles.IGL10 gl)
        {
            float[] scratch = new float[16];

            // Draw background color
            GLES20.GlClear(GLES20.GlColorBufferBit);

            //synchronized (this) {
            if (mUpdateST)
            {
                mSTexture.UpdateTexImage();
                mUpdateST = false;
            }
            //}

            GLES20.GlUseProgram(hProgram);

            int ph = GLES20.GlGetAttribLocation(hProgram, "vPosition");
            int tch = GLES20.GlGetAttribLocation(hProgram, "vTexCoord");
            int th = GLES20.GlGetUniformLocation(hProgram, "sTexture");

            GLES20.GlActiveTexture(GLES20.GlTexture0);
            GLES20.GlBindTexture(GLES11Ext.GlTextureExternalOes, hTex[0]);
            GLES20.GlUniform1i(th, 0);

            GLES20.GlVertexAttribPointer(ph, 2, GLES20.GlFloat, false, 4 * 2, pVertex);
            GLES20.GlVertexAttribPointer(tch, 2, GLES20.GlFloat, false, 4 * 2, pTexCoord);
            GLES20.GlEnableVertexAttribArray(ph);
            GLES20.GlEnableVertexAttribArray(tch);

            GLES20.GlDrawArrays(GLES20.GlTriangleStrip, 0, 4);


            // Set the camera position (View matrix)
            Android.Opengl.Matrix.SetLookAtM(mViewMatrix, 0, 0, 0, -3, 0f, 0f, 0f, 0f, 1.0f, 0.0f);

            // Calculate the projection and view transformation
            Android.Opengl.Matrix.MultiplyMM(mMVPMatrix, 0, mProjectionMatrix, 0, mViewMatrix, 0);


            // Create a rotation for the triangle

            // Use the following code to generate constant rotation.
            // Leave this code out when using TouchEvents.
            // long time = SystemClock.uptimeMillis() % 4000L;
            // float angle = 0.090f * ((int) time);

            Android.Opengl.Matrix.SetRotateM(mRotationMatrix, 0, mAngle, 0, 0, 1.0f);

            // Combine the rotation matrix with the projection and camera view
            // Note that the mMVPMatrix factor *must be first* in order
            // for the matrix multiplication product to be correct.
            Android.Opengl.Matrix.MultiplyMM(scratch, 0, mMVPMatrix, 0, mRotationMatrix, 0);

            // Draw triangle
            mTriangle.draw(scratch);
        }

        public void OnSurfaceChanged(Javax.Microedition.Khronos.Opengles.IGL10 gl, int width, int height)
        {
            // Adjust the viewport based on geometry changes,
            // such as screen rotation
            GLES20.GlViewport(0, 0, width, height);

            float ratio = (float)width / height;

            // this projection matrix is applied to object coordinates
            // in the onDrawFrame() method
            Android.Opengl.Matrix.FrustumM(mProjectionMatrix, 0, -ratio, ratio, -1, 1, 3, 7);

            GLES20.GlViewport(0, 0, width, height);
            if (mCamera != null)
            {
                Android.Hardware.Camera.Parameters p = mCamera.GetParameters();
                System.Collections.Generic.IList<Android.Hardware.Camera.Size> sizes = p.SupportedPreviewSizes;
                p.SetPreviewSize(sizes[0].Width, sizes[0].Height);
                mCamera.SetParameters(p);
                //mCamera.SetPreviewDisplay(holder);
                mCamera.StartPreview();
            }

        }

        public void OnSurfaceCreated(Javax.Microedition.Khronos.Opengles.IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            
            // Set the background frame color
            GLES20.GlClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            initTex();
            mSTexture = new SurfaceTexture(hTex[0]);
            mSTexture.SetOnFrameAvailableListener(this);

            mCamera = Android.Hardware.Camera.Open();
            try
            {
                mCamera.SetPreviewTexture(mSTexture);
            }
            catch (Exception ioe)
            {
            }

            GLES20.GlClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            hProgram = loadShader(vss, fss);

            mTriangle = new Triangle();
        }

        #region IOnFrameAvailableListener implementation

        public void OnFrameAvailable(SurfaceTexture surfaceTexture)
        {
            mUpdateST = true;
            mView.RequestRender();
        }

        #endregion


        /**
     * Utility method for compiling a OpenGL shader.
     *
     * <p><strong>Note:</strong> When developing shaders, use the checkGlError()
     * method to debug shader coding errors.</p>
     *
     * @param type - Vertex or fragment shader type.
     * @param shaderCode - String containing the shader code.
     * @return - Returns an id for the shader.
     */
        public static int loadShader(int type, String shaderCode)
        {

            // create a vertex shader type (GLES20.GL_VERTEX_SHADER)
            // or a fragment shader type (GLES20.GL_FRAGMENT_SHADER)
            int shader = GLES20.GlCreateShader(type);

            // add the source code to the shader and compile it
            GLES20.GlShaderSource(shader, shaderCode);
            GLES20.GlCompileShader(shader);

            return shader;
        }

        /**
     * Utility method for debugging OpenGL calls. Provide the name of the call
     * just after making it:
     *
     * <pre>
     * mColorHandle = GLES20.glGetUniformLocation(mProgram, "vColor");
     * MyGLRenderer.checkGlError("glGetUniformLocation");</pre>
     *
     * If the operation is not successful, the check throws an error.
     *
     * @param glOperation - Name of the OpenGL call to check.
     */
        public static void checkGlError(String glOperation)
        {
            int error;
            while ((error = GLES20.GlGetError()) != GLES20.GlNoError)
            {
                /*Log.e(TAG, glOperation + ": glError " + error);*/
                throw new Java.Lang.RuntimeException(glOperation + ": glError " + error);
            }
        }

        /**
     * Returns the rotation angle of the triangle shape (mTriangle).
     *
     * @return - A float representing the rotation angle.
     */
        public float getAngle()
        {
            return mAngle;
        }

        /**
     * Sets the rotation angle of the triangle shape (mTriangle).
     */
        public void setAngle(float angle)
        {
            mAngle = angle;
        }


        private void initTex()
        {
            hTex = new int[1];
            GLES20.GlGenTextures(1, hTex, 0);
            GLES20.GlBindTexture(GLES11Ext.GlTextureExternalOes, hTex[0]);
            GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
            GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureWrapT, GLES20.GlClampToEdge);
            GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureMinFilter, GLES20.GlNearest);
            GLES20.GlTexParameteri(GLES11Ext.GlTextureExternalOes, GLES20.GlTextureMagFilter, GLES20.GlNearest);
        }

        private void deleteTex()
        {
            GLES20.GlDeleteTextures(1, hTex, 0);
        }

        private static int loadShader(String vss, String fss)
        {
            int vshader = GLES20.GlCreateShader(GLES20.GlVertexShader);
            GLES20.GlShaderSource(vshader, vss);
            GLES20.GlCompileShader(vshader);
            int[] compiled = new int[1];
            GLES20.GlGetShaderiv(vshader, GLES20.GlCompileStatus, compiled, 0);
            if (compiled[0] == 0)
            {
                //Log.e("Shader", "Could not compile vshader");
                //Log.v("Shader", "Could not compile vshader:" + GLES20.glGetShaderInfoLog(vshader));
                GLES20.GlDeleteShader(vshader);
                vshader = 0;
            }

            int fshader = GLES20.GlCreateShader(GLES20.GlFragmentShader);
            GLES20.GlShaderSource(fshader, fss);
            GLES20.GlCompileShader(fshader);
            GLES20.GlGetShaderiv(fshader, GLES20.GlCompileStatus, compiled, 0);
            if (compiled[0] == 0)
            {
                /*Log.e("Shader", "Could not compile fshader");
            Log.v("Shader", "Could not compile fshader:" + GLES20.glGetShaderInfoLog(fshader));*/
                GLES20.GlDeleteShader(fshader);
                fshader = 0;
            }

            int program = GLES20.GlCreateProgram();
            GLES20.GlAttachShader(program, vshader);
            GLES20.GlAttachShader(program, fshader);
            GLES20.GlLinkProgram(program);

            return program;
        }

    }
}

