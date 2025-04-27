using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public static class CsvDataLoader
{
    // A single record describing where in the buffer each string lives
    public struct Record
    {
        public int barcodeOffset;
        public int barcodeLength;
        public int nucOffset;
        public int nucLength;
        public int protOffset;
        public int protLength;
    }

    /// <summary>
    /// Reads a CSV where lines are: barcode, nucleotideSequence, proteinSequence
    /// (no embedded commas). All text is assumed ASCII.
    /// </summary>
    public static void LoadCsv(
        string csvPath,
        out NativeArray<Record> records,
        out NativeArray<byte> buffer,
        Allocator allocator = Allocator.Persistent
    ) {
        var tempRecords = new NativeList<Record>(Allocator.Temp);
        var byteList    = new List<byte>(1024);   // give it a head start

        using(var reader = new StreamReader(csvPath))
        {
            string line;
            while((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');
                if(parts.Length != 3) continue;

                var rec = new Record();

                // barcode
                var bBytes = Encoding.ASCII.GetBytes(parts[0] ?? "");
                rec.barcodeOffset = byteList.Count;
                rec.barcodeLength = bBytes.Length;
                byteList.AddRange(bBytes);

                // nucleotide
                var nBytes = Encoding.ASCII.GetBytes(parts[1] ?? "");
                rec.nucOffset = byteList.Count;
                rec.nucLength = nBytes.Length;
                byteList.AddRange(nBytes);

                // protein
                var pBytes = Encoding.ASCII.GetBytes(parts[2] ?? "");
                rec.protOffset = byteList.Count;
                rec.protLength = pBytes.Length;
                byteList.AddRange(pBytes);

                tempRecords.Add(rec);
            }
        }

// now copy into NativeArray<Record>
        var recordView = tempRecords.AsArray();
        records = new NativeArray<Record>(recordView, allocator);

// and into NativeArray<byte>
        buffer = new NativeArray<byte>(byteList.Count, allocator, NativeArrayOptions.UninitializedMemory);
        buffer.CopyFrom(byteList.ToArray());

        tempRecords.Dispose();

    }

    /// <summary>
    /// Saves the two arrays into a compact .bin file:
    /// [int recordCount][int bufferLength]
    /// then that many Records, then that many bytes.
    /// </summary>
    public static void SaveBinary(
        string binPath,
        NativeArray<Record> records,
        NativeArray<byte> buffer
    ) {
        using (var bw = new BinaryWriter(File.Open(binPath, FileMode.Create)))
        {
            bw.Write(records.Length);
            bw.Write(buffer.Length);
            // write each struct
            for (int i = 0; i < records.Length; i++)
            {
                var r = records[i];
                bw.Write(r.barcodeOffset);
                bw.Write(r.barcodeLength);
                bw.Write(r.nucOffset);
                bw.Write(r.nucLength);
                bw.Write(r.protOffset);
                bw.Write(r.protLength);
            }
            // write raw bytes
            bw.Write(buffer.ToArray());
        }
    }

    /// <summary>
    /// Loads back the .bin into new NativeArrays.
    /// </summary>
    public static void LoadBinary(
        string binPath,
        out NativeArray<Record> records,
        out NativeArray<byte> buffer,
        Allocator allocator = Allocator.Persistent
    ) {
        using (var br = new BinaryReader(File.Open(binPath, FileMode.Open)))
        {
            int recCount   = br.ReadInt32();
            int bufLength  = br.ReadInt32();

            records = new NativeArray<Record>(recCount, allocator, NativeArrayOptions.UninitializedMemory);
            buffer  = new NativeArray<byte>(bufLength, allocator, NativeArrayOptions.UninitializedMemory);

            // read records
            for (int i = 0; i < recCount; i++)
            {
                var r = new Record {
                    barcodeOffset = br.ReadInt32(),
                    barcodeLength = br.ReadInt32(),
                    nucOffset     = br.ReadInt32(),
                    nucLength     = br.ReadInt32(),
                    protOffset    = br.ReadInt32(),
                    protLength    = br.ReadInt32()
                };
                records[i] = r;
            }

            // read byte buffer
            var raw = br.ReadBytes(bufLength);
            buffer.CopyFrom(raw);
        }
    }

    /// <summary>
    /// Example job that searches for a target substring in the nucleotide sequences.
    /// </summary>
    [BurstCompile]
    public struct SearchJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Record> records;
        [ReadOnly] public NativeArray<byte>   buffer;
        [ReadOnly] public NativeArray<byte>   target; // ASCII bytes of the substring to search

        [WriteOnly] public NativeArray<int>   results; // 1 if found, 0 otherwise

        public void Execute(int index)
        {
            var rec = records[index];
            int seqStart = rec.protOffset;
            int seqLen   = rec.protLength;

            // simple brute-force substring search
            for (int i = 0; i <= seqLen - target.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < target.Length; j++)
                {
                    if (buffer[seqStart + i + j] != target[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    results[index] = 1;
                    return;
                }
            }
            results[index] = 0;
        }
    }

    // Utility to turn a C# string into a NativeArray<byte>
    public static NativeArray<byte> StringToBytes(string s, Allocator alloc)
    {
        var b = Encoding.ASCII.GetBytes(s);
        var na = new NativeArray<byte>(b.Length, alloc, NativeArrayOptions.UninitializedMemory);
        na.CopyFrom(b);
        return na;
    }
}
