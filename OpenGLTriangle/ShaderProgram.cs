﻿using System;
using System.IO;
using System.Reflection;
using Silk.NET.OpenGL;
// ReSharper disable InconsistentNaming

namespace OpenGLTriangle
{
    public class ShaderProgram : IDisposable
    {
        private static readonly string BaseDirPath = @$"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\";
        private readonly uint _programId;
        private readonly uint _vertexShaderId;
        private readonly uint _fragmentShaderId;
        private bool _isDisposed;
        private readonly GL _gl;

        /// <summary>
        /// Creates a new instance of <see cref="ShaderProgram"/>.
        /// </summary>
        /// <param name="openGL">The OpenGL object to call OpenGL functions.</param>
        /// <param name="vertexShaderName">The name of the vertex shader.</param>
        /// <param name="fragmentShaderName">The name of the fragment shader.</param>
        /// <exception cref="Exception">Thrown if there is an issue create the shader program.</exception>
        public ShaderProgram(GL openGL, string vertexShaderName, string fragmentShaderName)
        {
            _gl = openGL;
            _vertexShaderId = LoadVertShader(vertexShaderName);
            _fragmentShaderId = LoadFragShader(fragmentShaderName);

            _programId = _gl.CreateProgram();

            _gl.AttachShader(_programId, _vertexShaderId);
            _gl.AttachShader(_programId, _fragmentShaderId);
            _gl.LinkProgram(_programId);
            _gl.ValidateProgram(_programId);

            // Check for linking errors
            _gl.GetProgram(_programId, ProgramPropertyARB.LinkStatus, out var progParams);

            if (progParams > 0) return;

            // We can use `this.gl.GetProgramInfoLog(program)` to get information about the error.
            var programInfoLog = _gl.GetProgramInfoLog(_programId);

            throw new Exception($"Error occurred while linking program with ID '{_programId}'\n{programInfoLog}");
        }

        ~ShaderProgram() => Dispose();

        public void Use() => _gl.UseProgram(_programId);

        public void StopUsing() => _gl.UseProgram(0);

        private uint LoadVertShader(string name)
        {
            name = Path.HasExtension(name)
                ? Path.GetFileNameWithoutExtension(name)
                : name;

            var fullFilepath = $"{BaseDirPath}{name}.vert";

            var shaderSrc = File.ReadAllText(fullFilepath);

            var shaderId = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(shaderId, shaderSrc);
            _gl.CompileShader(shaderId);

            _gl.GetShader(shaderId, GLEnum.CompileStatus, out var status);
            if (status == 0)
            {
                throw new Exception("Error compiling vertex shader");
            }

            return shaderId;
        }

        private uint LoadFragShader(string name)
        {
            name = Path.HasExtension(name)
                ? Path.GetFileNameWithoutExtension(name)
                : name;

            var fullFilepath = $"{BaseDirPath}{name}.frag";
            var shaderSrc = File.ReadAllText(fullFilepath);

            var shaderId = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(shaderId, shaderSrc);
            _gl.CompileShader(shaderId);

            _gl.GetShader(shaderId, GLEnum.CompileStatus, out var status);
            if (status == 0)
            {
                throw new Exception("Error compiling fragment shader");
            }

            return shaderId;
        }

        /// <summary>
        /// <inheritdoc cref="IDisposable.Dispose"/>.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed is false)
            {
                return;
            }

            StopUsing();
            _gl.DetachShader(_programId, _vertexShaderId);
            _gl.DetachShader(_programId, _fragmentShaderId);
            _gl.DeleteShader(_vertexShaderId);
            _gl.DeleteShader(_fragmentShaderId);
            _gl.DeleteProgram(_programId);

            _isDisposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
