using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Runtime.InteropServices;

namespace InstancedModelRendering
{
    public class Game1 : Game
    {
        GraphicsDeviceManager _graphics;
        Model model;
        Effect effect;
        DynamicVertexBuffer instanceBuffer;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void LoadContent()
        {
            model = Content.Load<Model>("Model");
            effect = Content.Load<Effect>("Effect");

            Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 8), Vector3.Zero, Vector3.Up);
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(
                               MathHelper.ToRadians(45f),
                               (float)_graphics.PreferredBackBufferWidth / _graphics.PreferredBackBufferHeight,
                               1f, 1000f);

            effect.Parameters["ViewProjection"].SetValue(view * projection);
            effect.Parameters["LightDir"].SetValue(Vector3.Normalize(new Vector3(1, 2, 3)));

            instanceBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(VertexInstanceTransform), 3, BufferUsage.WriteOnly);
            instanceBuffer.SetData(new VertexInstanceTransform[]
            {
                new(Matrix.Transpose(Matrix.CreateTranslation(0, -2, 0))),
                new(Matrix.Transpose(Matrix.CreateTranslation(0, 0, 0))),
                new(Matrix.Transpose(Matrix.CreateTranslation(0, 2, 0))),
            });
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            DrawModel(false, new Vector3(-2, 0, 0));

            DrawModel(true, new Vector3(2, 0, 0));

            base.Draw(gameTime);
        }

        private void DrawModel(bool instanced, Vector3 position)
        {
            string technqiueName = instanced ? "RenderInstanced" : "Render";
            effect.CurrentTechnique = effect.Techniques[technqiueName];

            foreach (var mesh in model.Meshes)
            {
                effect.Parameters["World"].SetValue(mesh.ParentBone.Transform 
                    * Matrix.CreateScale(0.01f) /* scale down */
                    * Matrix.CreateTranslation(position));

                foreach (var meshPart in mesh.MeshParts)
                {
                    effect.Parameters["ColorTex"].SetValue(((BasicEffect)meshPart.Effect).Texture);

                    // set buffers
                    GraphicsDevice.Indices = meshPart.IndexBuffer;

                    if (instanced)
                    {
                        GraphicsDevice.SetVertexBuffers(
                            new VertexBufferBinding(meshPart.VertexBuffer),
                            new VertexBufferBinding(instanceBuffer, 0, 1));
                    }
                    else
                    {
                        GraphicsDevice.SetVertexBuffer(meshPart.VertexBuffer);
                    }

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();

                        if (instanced)
                        {
                            // This doesn't work as expected for offsets larger than 0
                            GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList,
                                meshPart.VertexOffset,
                                meshPart.StartIndex,
                                meshPart.PrimitiveCount,
                                instanceBuffer.VertexCount);
                        }
                        else
                        {
                            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                meshPart.VertexOffset,
                                meshPart.StartIndex,
                                meshPart.PrimitiveCount);
                        }
                    }
                }
            }
        }
    }



    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexInstanceTransform : IVertexType
    {
        public Matrix Transform;

        public VertexInstanceTransform(Matrix transform)
        {
            Transform = transform;
        }

        private static readonly VertexDeclaration vertexDeclaration;
        static VertexInstanceTransform()
        {
            VertexElement[] elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
                new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3),
                new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4),
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            vertexDeclaration = declaration;
        }

        public VertexDeclaration VertexDeclaration => vertexDeclaration;

        public override int GetHashCode()
        {
            unchecked
            {
                return Transform.GetHashCode();
            }
        }
    }
}