var faceByte1 = File.ReadAllBytes("./faces/face1.png");
var faceByte2 = File.ReadAllBytes("./faces/face2.png");

Console.WriteLine("{sync 1 sample test}");
var compareData = ArcFacePackage.ArcFacePackage.Process(faceByte1, faceByte2);
Console.WriteLine($"Distance {compareData[0]}; Similarity {compareData[1]}");

Console.WriteLine("{async 1 sample test}");
compareData = await ArcFacePackage.ArcFacePackage.ProcessAsync(faceByte1, faceByte2, CancellationToken.None);
Console.WriteLine($"Distance {compareData[0]}; Similarity {compareData[1]}");

Console.WriteLine("{sync 10 sample time test}");
var syncOutput = new float[10][];
var syncWatch = System.Diagnostics.Stopwatch.StartNew();
for (int i = 0; i < syncOutput.Length; i++)
{
    syncOutput[i] = ArcFacePackage.ArcFacePackage.Process(faceByte1, faceByte2);
}
syncWatch.Stop();
var syncElapsedMs = syncWatch.ElapsedMilliseconds;
foreach (var output in syncOutput)
{
    Console.WriteLine($"Distance {output[0]}; Similarity {output[1]}");
}
Console.WriteLine($"in {syncElapsedMs} ms");

Console.WriteLine("{async 10 sample time test}");
var asyncOutput = new Task<float[]>[10];
var asyncWatch = System.Diagnostics.Stopwatch.StartNew();
for (int i = 0; i < asyncOutput.Length; i++)
{
    asyncOutput[i] = ArcFacePackage.ArcFacePackage.ProcessAsync(faceByte1, faceByte2, CancellationToken.None);
}
Task.WaitAll(asyncOutput);
asyncWatch.Stop();
var asyncElapsedMs = asyncWatch.ElapsedMilliseconds;
foreach (var output in asyncOutput)
{
    Console.WriteLine($"Distance {output.Result[0]}; Similarity {output.Result[1]}");
}
Console.WriteLine($"in {asyncElapsedMs} ms");

Console.WriteLine("{async 1s 10 samples timer test}");
CancellationTokenSource cancelTokenSource = new();
CancellationToken token = cancelTokenSource.Token;
asyncOutput = new Task<float[]>[10];
try
{
    for (int i = 0; i < asyncOutput.Length; i++)
    {
        asyncOutput[i] = Task.Run(() => ArcFacePackage.ArcFacePackage.ProcessAsync(faceByte1, faceByte2, token));
    }
    Thread.Sleep((int)(asyncElapsedMs / 3));
    cancelTokenSource.Cancel();
    Thread.Sleep((int)(asyncElapsedMs * 1.5));
} catch (Exception ex) 
{
    Console.WriteLine(ex.Message);
}
foreach (var output in asyncOutput)
{
    Console.WriteLine($"{output.Status}");
}