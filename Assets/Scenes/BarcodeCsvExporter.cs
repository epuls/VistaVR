using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class BarcodeCsvExporter : MonoBehaviour
{
    [Tooltip("Path to your converted .bin file")]
    public string binPath = "Library/huge_data.bin";
    [Tooltip("Substring to search for")]
    public string target = "AGCTTAGG";
    [Tooltip("Where to write the matching barcodes CSV")]
    public string outputCsvPath = "Library/matching_barcodes.csv";
    [Tooltip("How many records to process per frame")]
    public int recordsPerBatch = 10_000;

    void Start()
    {
        StartCoroutine(ExportMatchingBarcodes());
    }

    private IEnumerator ExportMatchingBarcodes()
    {
        // Open reader & writer
        using var reader = new BinaryReader(File.Open(binPath, FileMode.Open), Encoding.ASCII);
        using var writer = new StreamWriter(outputCsvPath, false, Encoding.ASCII);

        // Write CSV header
        writer.WriteLine("barcode");

        // Prepare target ASCII bytes for the Job
        var targetBytes = CsvDataLoader.StringToBytes(target, Allocator.TempJob);

        long totalFound = 0;
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            // 1) Build one batch of Record structs + raw-buffer
            var recs = new NativeList<CsvDataLoader.Record>(recordsPerBatch, Allocator.TempJob);
            var buf  = new NativeList<byte>(1, Allocator.TempJob); // init capacity=1 so Add() never nulls

            int actuallyRead = 0;
            for (; actuallyRead < recordsPerBatch 
                   && reader.BaseStream.Position < reader.BaseStream.Length; 
                   actuallyRead++)
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

                // read all bytes for this record
                byte[] all = reader.ReadBytes(bl + nl + pl);
                // manually append into NativeList<byte>
                for (int j = 0; j < all.Length; j++)
                    buf.Add(all[j]);
            }

            // 2) Fire off the Burst job on this batch
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

            // 3) Extract matching barcodes and write to CSV
            for (int i = 0; i < actuallyRead; i++)
            {
                if (results[i] == 1)
                {
                    var r = recordsNA[i];
                    int off = r.barcodeOffset;
                    int len = r.barcodeLength;
                    // copy out the barcode bytes
                    byte[] barBytes = new byte[len];
                    for (int k = 0; k < len; k++)
                        barBytes[k] = bufferNA[off + k];
                    // turn into ASCII string
                    string barcodeStr = Encoding.ASCII.GetString(barBytes);
                    writer.WriteLine(barcodeStr);
                    totalFound++;
                }
            }

            // 4) Cleanup native arrays & give Unity a frame
            recordsNA.Dispose();
            bufferNA.Dispose();
            results.Dispose();

            yield return null;
        }

        targetBytes.Dispose();
        Debug.Log($"✅ Exported {totalFound:n0} matching barcodes to `{outputCsvPath}`");
    }
}
