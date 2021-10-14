using System;
using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace OpenGLTriangle
{
    public class Game
    {
        // ReSharper disable once InconsistentNaming
        private static GL? GL;
        private static DebugProc? _debugCallback;
        private readonly IWindow _glWindow;
        private ShaderProgram? _shader;

        private readonly float[] _vertices = new []
        {
            // Top Left Triangle
            -0.5f,  0.5f, 0.0f, // Top Left Vert
            -0.5f, -0.5f, 0.0f, // Bottom Left Vert
             0.5f,  0.5f, 0.0f, // Top Right Vert
        };

        private uint _vao; // Vertex Array Object
        private uint _vbo; // Vertex Buffer Object
        private readonly Glfw _glfw;

        /// <summary>
        /// Creates a new instance of <see cref="Game"/>.
        /// </summary>
        public Game()
        {
            var options = WindowOptions.Default;
            var api = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(4, 5))
            {
                Profile = ContextProfile.Core
            };

            options.API = api;
            options.ShouldSwapAutomatically = false;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "LearnOpenGL with Silk.NET";
            options.Position = new Vector2D<int>(400, 400);
            _glWindow = Window.Create(options);

            _glWindow.Load += OnLoad;
            _glWindow.Render += OnRender;
            _glWindow.Closing += OnClose;
            _glWindow.Title = "Simple Quad";
        }

        /// <summary>
        /// Loads the game content.
        /// </summary>
        private unsafe void OnLoad()
        {
            // Must be called in the on load.  The OpenGL context must be created first
            // and that does not occur until the onload method has been invoked
            GL = GL.GetApi(_glWindow);
            SetupErrorCallback();

            _shader = new ShaderProgram(GL, "shader", "shader");

            // Generate the VAO and VBO with only 1 object each
            GL?.GenVertexArrays(1, out _vao);
            GL?.GenBuffers(1, out _vbo);

            // Make the VAO the current Vertex Array Object by binding it
            GL?.BindVertexArray(_vao);

            // Bind the VBO specifying it's a GL_ARRAY_BUFFER
            GL?.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

            // Introduce the vertices into the VBO
            fixed (void* data = _vertices)
            {
                GL?.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(float) * _vertices.Length), data, BufferUsageARB.StaticDraw);
            }

            // Configure the Vertex Attribute so that OpenGL knows how to read the VBO
            GL?.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

            // Enable the Vertex Attribute so that OpenGL knows to use it
            GL?.EnableVertexAttribArray(0);

            // Bind both the VBO and VAO to 0 so that we don't accidentally modify the VAO and VBO we created
            GL?.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            GL?.BindVertexArray(0);
        }

        private void OnRender(double obj)
        {
            // Specify the color of the background
            GL?.ClearColor(0.07f, 0.13f, 0.17f, 1.0f);

            // Clean the back buffer and assign the new color to it
            GL?.Clear(ClearBufferMask.ColorBufferBit);

            // Tell OpenGL which Shader Program we want to use
            _shader?.Use();

            // Bind the VAO so OpenGL knows to use it
            GL?.BindVertexArray(_vao);

            // Draw the triangle using the GL_TRIANGLES primitive
            GL?.DrawArrays(PrimitiveType.Triangles, 0, 3);

            // Swap the back buffer with the front buffer
            _glWindow.SwapBuffers();
        }

        private void OnClose()
        {
            GL?.DeleteVertexArray(_vao);
            GL?.DeleteBuffer(_vbo);
            _shader?.Dispose();
        }

        /// <summary>
        /// Runs the game.
        /// </summary>
        public void Run() => _glWindow.Run();

        /// <summary>
        /// Setup the callback to be invoked when OpenGL encounters an internal error.
        /// </summary>
        private void SetupErrorCallback()
        {
            if (_debugCallback != null) return;

            _debugCallback = DebugCallback;

            /*NOTE:
             * This is here to help prevent an issue with an obscure System.ExecutionException from occurring.
             * The garbage collector performs a collect on the delegate passed into GL?.DebugMesageCallback()
             * without the native system knowing about it which causes this exception. The GC.KeepAlive()
             * method tells the garbage collector to not collect the delegate to prevent this from happening.
             */
            GC.KeepAlive(_debugCallback);

            GL?.DebugMessageCallback(_debugCallback, Marshal.StringToHGlobalAnsi(string.Empty));
        }

        /// <summary>
        /// Throws an exception when error OpenGL errors occur.
        /// </summary>
        /// <exception cref="Exception">The OpenGL message as an exception.</exception>
        private void DebugCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userParam)
        {
            var errorMessage = Marshal.PtrToStringAnsi(message);

            errorMessage += $"\n\tSrc: {source}";
            errorMessage += $"\n\tType: {type}";
            errorMessage += $"\n\tID: {id}";
            errorMessage += $"\n\tSeverity: {severity}";
            errorMessage += $"\n\tLength: {length}";
            errorMessage += $"\n\tUser Param: {Marshal.PtrToStringAnsi(userParam)}";

            if (severity != GLEnum.DebugSeverityNotification)
            {
                throw new Exception(errorMessage);
            }
        }
    }
}
