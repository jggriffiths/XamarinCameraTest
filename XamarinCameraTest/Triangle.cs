using System;
using Java.Nio;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES11;
using OpenTK.Graphics.ES20;
using Android.Opengl;

/*using GL1 = OpenTK.Graphics.ES11.GL;
using All1 = OpenTK.Graphics.ES11.All;
using GL2 = OpenTK.Graphics.ES20.GL;*/

namespace XamarinCameraTest
{
    public class Triangle
    {
        private readonly string vertexShaderCode =
            // This matrix member variable provides a hook to manipulate
            // the coordinates of the objects that use this vertex shader
            "uniform mat4 uMVPMatrix;" +
            "attribute vec4 vPosition;" +
            "void main() {" +
                    // the matrix must be included as a modifier of gl_Position
                    // Note that the uMVPMatrix factor *must be first* in order
                    // for the matrix multiplication product to be correct.
            "  gl_Position = uMVPMatrix * vPosition;" +
            "}";

        private readonly string fragmentShaderCode =
            "precision mediump float;" +
            "uniform vec4 vColor;" +
            "void main() {" +
            "  gl_FragColor = vColor;" +
            "}";

        private readonly FloatBuffer vertexBuffer;
        private readonly int mProgram;
        private int mPositionHandle;
        private int mColorHandle;
        private int mMVPMatrixHandle;

        // number of coordinates per vertex in this array
        private int COORDS_PER_VERTEX = 3;
        private float[] triangleCoords =
            {
                // in counterclockwise order:
                0.0f,  0.622008459f, 0.0f,   // top
                -0.5f, -0.311004243f, 0.0f,   // bottom left
                0.5f, -0.311004243f, 0.0f    // bottom right
            };
        private int vertexCount = 9 / 3;
        private int vertexStride = 3 * 4;
        // 4 bytes per vertex

        float[] color = { 0.63671875f, 0.76953125f, 0.22265625f, 0.0f };

        /**
     * Sets up the drawing object data for use in an OpenGL ES context.
     */
        public Triangle()
        {
            // initialize vertex byte buffer for shape coordinates
            ByteBuffer bb = ByteBuffer.AllocateDirect(
                // (number of coordinate values * 4 bytes per float)
                                triangleCoords.Length * 4);
            // use the device hardware's native byte order
            bb.Order(ByteOrder.NativeOrder());

            // create a floating point buffer from the ByteBuffer
            vertexBuffer = bb.AsFloatBuffer();
            // add the coordinates to the FloatBuffer
            vertexBuffer.Put(triangleCoords);
            // set the buffer to read the first coordinate
            vertexBuffer.Position(0);

            // prepare shaders and OpenGL program
            int vertexShader = MainRenderer.loadShader(GLES20.GlVertexShader, vertexShaderCode);
            int fragmentShader = MainRenderer.loadShader(GLES20.GlFragmentShader, fragmentShaderCode);

            mProgram = GLES20.GlCreateProgram();             // create empty OpenGL Program
            GLES20.GlAttachShader(mProgram, vertexShader);   // add the vertex shader to program
            GLES20.GlAttachShader(mProgram, fragmentShader); // add the fragment shader to program
            GLES20.GlLinkProgram(mProgram);                  // create OpenGL program executables

        }

        /**
     * Encapsulates the OpenGL ES instructions for drawing this shape.
     *
     * @param mvpMatrix - The Model View Project matrix in which to draw
     * this shape.
     */
        public void draw(float[] mvpMatrix)
        {
            // Add program to OpenGL environment
            GLES20.GlUseProgram(mProgram);

            // get handle to vertex shader's vPosition member
            mPositionHandle = GLES20.GlGetAttribLocation(mProgram, "vPosition");

            // Enable a handle to the triangle vertices
            GLES20.GlEnableVertexAttribArray(mPositionHandle);

            // Prepare the triangle coordinate data
            GLES20.GlVertexAttribPointer(
                mPositionHandle, COORDS_PER_VERTEX,
                GLES20.GlFloat, false,
                vertexStride, vertexBuffer);

            // get handle to fragment shader's vColor member
            mColorHandle = GLES20.GlGetUniformLocation(mProgram, "vColor");

            // Set color for drawing the triangle
            GLES20.GlUniform4fv(mColorHandle, 1, color, 0);

            // get handle to shape's transformation matrix
            mMVPMatrixHandle = GLES20.GlGetUniformLocation(mProgram, "uMVPMatrix");
            MainRenderer.checkGlError("glGetUniformLocation");

            // Apply the projection and view transformation
            GLES20.GlUniformMatrix4fv(mMVPMatrixHandle, 1, false, mvpMatrix, 0);
            MainRenderer.checkGlError("glUniformMatrix4fv");

            // Draw the triangle
            GLES20.GlDrawArrays(GLES20.GlTriangles, 0, vertexCount);

            // Disable vertex array
            GLES20.GlDisableVertexAttribArray(mPositionHandle);
        }
    }
}
