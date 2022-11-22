using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ArcFacePackage
{
    public static class ArcFacePackage
    {
        private static readonly InferenceSession Session = GetSession();

        public static InferenceSession GetSession()
        {
            var assembly = typeof(ArcFacePackage).Assembly;
            using var modelStream = assembly.GetManifestResourceStream("ArcFacePackage.arcfaceresnet100-8.onnx");
            using var memoryStream = new MemoryStream();
            if (modelStream is not null) { modelStream.CopyTo(memoryStream); }
            return new InferenceSession(memoryStream.ToArray());
        }

        public static float[] Process(byte[] image1, byte[] image2)
        {
            var embeddings1 = GetEmbeddings(image1);
            var embeddings2 = GetEmbeddings(image2);

            var distance = Distance(embeddings1, embeddings2) * Distance(embeddings1, embeddings2);
            var similarity = Similarity(embeddings1, embeddings2);
            return new float[] { distance, similarity };
        }

        public static async Task<float[]> ProcessAsync(byte[] image1, byte[] image2, CancellationToken token)
        {
            Task<float[]> embeddings1 = GetEmbeddings(image1, token);
            Task<float[]> embeddings2 = GetEmbeddings(image2, token);

            await Task.WhenAll(embeddings1, embeddings2);

            var distance = Distance(embeddings1.Result, embeddings2.Result) * Distance(embeddings1.Result, embeddings2.Result);
            var similarity = Similarity(embeddings1.Result, embeddings2.Result);
            return new float[] { distance, similarity };
        }

        private static float Length(float[] v) => (float)Math.Sqrt(v.Select(x => x * x).Sum());

        private static float Distance(float[] v1, float[] v2) => Length(v1.Zip(v2).Select(p => p.First - p.Second).ToArray());

        private static float Similarity(float[] v1, float[] v2) => v1.Zip(v2).Select(p => p.First * p.Second).Sum();

        private static float[] Normalize(float[] v)
        {
            var len = Length(v);
            return v.Select(x => x / len).ToArray();
        }

        private static DenseTensor<float> ImageToTensor(Image<Rgb24> img, CancellationToken token)
        {
            var w = img.Width;
            var h = img.Height;
            var t = new DenseTensor<float>(new[] { 1, 3, h, w });

            img.ProcessPixelRows(pa =>
            {
                for (int y = 0; y < h; y++)
                {
                    Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                    for (int x = 0; x < w; x++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                        }
                        t[0, 0, y, x] = pixelSpan[x].R;
                        t[0, 1, y, x] = pixelSpan[x].G;
                        t[0, 2, y, x] = pixelSpan[x].B;
                    }
                }
            });

            return t;
        }

        private static DenseTensor<float> ImageToTensor(Image<Rgb24> img)
        {
            var w = img.Width;
            var h = img.Height;
            var t = new DenseTensor<float>(new[] { 1, 3, h, w });

            img.ProcessPixelRows(pa =>
            {
                for (int y = 0; y < h; y++)
                {
                    Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                    for (int x = 0; x < w; x++)
                    {
                        t[0, 0, y, x] = pixelSpan[x].R;
                        t[0, 1, y, x] = pixelSpan[x].G;
                        t[0, 2, y, x] = pixelSpan[x].B;
                    }
                }
            });

            return t;
        }

        private static async Task<float[]> GetEmbeddings(byte[] byteImage, CancellationToken token)
        {
            MemoryStream ms = new(byteImage);
            Image<Rgb24> returnImage = await Image.LoadAsync<Rgb24>(ms, token);
            DenseTensor<float> tensor = ImageToTensor(returnImage, token);
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", tensor) };
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = Session.Run(inputs);
            return Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
        }

        private static float[] GetEmbeddings(byte[] byteImage)
        {
            MemoryStream ms = new(byteImage);
            Image<Rgb24> returnImage = Image.Load<Rgb24>(ms);
            DenseTensor<float> tensor = ImageToTensor(returnImage);
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", tensor) };
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = Session.Run(inputs);
            return Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
        }
    }
}