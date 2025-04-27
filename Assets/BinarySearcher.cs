using System;
using System.Collections;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class BinarySearcher : MonoBehaviour
{
    [Tooltip("Path to your converted .bin")]
    public string binPath;
    [Tooltip("Substring to look for")]
    public string target = "AGCTTAGG";
    [Tooltip("Records per batch before yielding")]
    public int recordsPerBatch = 10_000;

    void Start()
    {
        StartCoroutine(SearchBinaryCoroutine(foundCount => {
            Debug.Log($"🔎 Search complete – found in {foundCount:n0} records.");
        }));
    }

    private IEnumerator SearchBinaryCoroutine(Action<long> onComplete)
    {
        // Prepare the ASCII target in a temp job array
        var targetBytes = CsvDataLoader.StringToBytes(target, Allocator.TempJob);
        long totalFound = 0;

        using var reader = new BinaryReader(File.Open(binPath, FileMode.Open), System.Text.Encoding.ASCII);

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            // 1) Pre-allocate at least one slot so the first Add() never hits a null buffer
            var recs = new NativeList<CsvDataLoader.Record>(recordsPerBatch, Allocator.TempJob);
            var buf  = new NativeList<byte>(1, Allocator.TempJob);

            int actuallyRead = 0;
            for (; actuallyRead < recordsPerBatch && reader.BaseStream.Position < reader.BaseStream.Length; actuallyRead++)
            {
                int bl = reader.ReadInt32();
                int nl = reader.ReadInt32();
                int pl = reader.ReadInt32();

                var rec = new CsvDataLoader.Record
                {
                    barcodeOffset = buf.Length,
                    barcodeLength = bl,
                    nucOffset     = buf.Length + bl,
                    nucLength     = nl,
                    protOffset    = buf.Length + bl + nl,
                    protLength    = pl
                };
                recs.Add(rec);

                // read the raw bytes for this record in one go
                byte[] all = reader.ReadBytes(bl + nl + pl);
                for (int j = 0; j < all.Length; j++)
                    buf.Add(all[j]);
            }

            // Fire off a Burst job over this batch
            var recordsNA = new NativeArray<CsvDataLoader.Record>(recs.AsArray(), Allocator.TempJob);
            var bufferNA  = new NativeArray<byte>(buf.AsArray(), Allocator.TempJob);
            recs.Dispose();
            buf.Dispose();

            var results = new NativeArray<int>(actuallyRead, Allocator.TempJob);
            var job = new CsvDataLoader.SearchJob
            {
                records = recordsNA,
                buffer  = bufferNA,
                target  = targetBytes,
                results = results
            };
            var handle = job.Schedule(actuallyRead, 64);
            handle.Complete();

            // tally
            for (int i = 0; i < actuallyRead; i++)
                if (results[i] == 1)
                    totalFound++;

            // cleanup
            recordsNA.Dispose();
            bufferNA.Dispose();
            results.Dispose();

            // give Unity a frame
            yield return null;
        }

        targetBytes.Dispose();
        onComplete?.Invoke(totalFound);
    }
}
