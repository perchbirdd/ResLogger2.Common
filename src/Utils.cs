﻿using System.IO.Hashing;
using System.Text;
using Newtonsoft.Json;
using ResLogger2.Common.Api;

namespace ResLogger2.Common;

public static class Utils
{
	private static readonly string _schema =
		@"{'type':'object','properties':{'Entries':{'type':'array','items':{'type':'string'}}},'required':['Entries']}";
	// private static readonly JSchema _jSchema = JSchema.Parse(_schema);

	private const string COMMON = "com"; 
	private const string BGCOMMON = "bgc";
	private const string BG = "bg/";
	private const string CUT = "cut";
	private const string CHARA = "cha";
	private const string SHADER = "sha";
	private const string UI = "ui/";
	private const string SOUND = "sou";
	private const string VFX = "vfx";
	private const string UI_SCRIPT = "ui_";
	private const string EXD = "exd";
	private const string GAME_SCRIPT = "gam";
	private const string MUSIC = "mus";
	private const string SQPACK_TEST = "_sq";
	private const string DEBUG = "_de";

	public static uint GetCategoryIdForPath(string gamePathStr)
	{
		return GetCategoryIdForPath(gamePathStr.AsSpan());
	}
	
	public static uint GetCategoryIdForPath(ReadOnlySpan<char> gamePath)
	{
		return gamePath switch
		{
			_ when gamePath.StartsWith(COMMON) => 0x000000,
			_ when gamePath.StartsWith(BGCOMMON) => 0x010000,
			_ when gamePath.StartsWith(BG) => GetBgSubCategoryId(gamePath) | (0x02 << 16),
			_ when gamePath.StartsWith(CUT) => GetNonBgSubCategoryId(gamePath, 4) | (0x03 << 16),
			_ when gamePath.StartsWith(CHARA) => 0x040000,
			_ when gamePath.StartsWith(SHADER) => 0x050000,
			_ when gamePath.StartsWith(UI) => 0x060000,
			_ when gamePath.StartsWith(SOUND) => 0x070000,
			_ when gamePath.StartsWith(VFX) => 0x080000,
			_ when gamePath.StartsWith(UI_SCRIPT) => 0x090000,
			_ when gamePath.StartsWith(EXD) => 0x0A0000,
			_ when gamePath.StartsWith(GAME_SCRIPT) => 0x0B0000,
			_ when gamePath.StartsWith(MUSIC) => GetNonBgSubCategoryId(gamePath, 6) | (0x0C << 16),
			_ when gamePath.StartsWith(SQPACK_TEST) => 0x120000,
			_ when gamePath.StartsWith(DEBUG) => 0x130000,
			_ => 0,
		};
	}

	private static uint GetBgSubCategoryId(ReadOnlySpan<char> gamePath)
	{
		var segmentIdIndex = 3;
		uint expacId = 0;

		// Check if this is an ex* path
		if (gamePath[3] != 'e')
			return 0;

		// Check if our expac ID has one or two digits
		if (gamePath[6] == '/')
		{
			expacId = uint.Parse(gamePath[5..6]) << 8;
			segmentIdIndex = 7;
		}
		else if (gamePath[7] == '/')
		{
			expacId = uint.Parse(gamePath[5..7]) << 8;
			segmentIdIndex = 8;
		}
		else
		{
			expacId = 0;
		}

		// Parse the segment id for this bg path
		var segmentId = uint.Parse(gamePath.Slice(segmentIdIndex, 2));
		
		return expacId + segmentId;
	}

	private static uint GetNonBgSubCategoryId(ReadOnlySpan<char> gamePath, int firstDirLen)
	{
		if (gamePath[firstDirLen] != 'e')
			return 0;

		if (gamePath[firstDirLen + 3] == '/')
			return uint.Parse(gamePath.Slice(firstDirLen + 2, 1)) << 8;

		if (gamePath[firstDirLen + 4] == '/')
			return uint.Parse(gamePath.Slice(firstDirLen + 2, 2)) << 8;

		return 0;
	}

	public static uint CalcFullHash(string pathStr)
	{
		var path = pathStr.AsSpan();
		return Crc32.Get(path);
	}
	
	public static (uint folderHash, uint fileHash) CalcHashes(string pathStr)
	{
		var pathLower = pathStr.ToLowerUnsafe();
		var path = pathLower.AsSpan();
		var splitter = path.LastIndexOf('/');
		var folderStr = path[..splitter];
		var fileStr = path[(splitter + 1)..];
		
		var folderHash = Crc32.Get(folderStr);
		var fileHash = Crc32.Get(fileStr);
		
		return (folderHash, fileHash);
	}

	public static (uint folderHash, uint fileHash, uint fullHash) CalcAllHashes(string path)
	{
		var parts = CalcHashes(path);
		return (parts.folderHash, parts.fileHash, CalcFullHash(path));
	}

	public static UploadedDbData GetUploadDataObjectFromString(string content)
	{
		return JsonConvert.DeserializeObject<UploadedDbData>(content);
	}
	
	public static ulong CalcExtendedHashWithLower(string fullText)
	{
		var tText = fullText.ToLowerInvariant();
		Span<byte> data = stackalloc byte[tText.Length];
		Encoding.ASCII.GetBytes(tText, data);
		Span<byte> hash = stackalloc byte[8];
		Crc64.Hash(data, hash);
		return BitConverter.ToUInt64(hash);
	}
	
	public static ulong CalcExtendedHash(string fullText)
	{
		Span<byte> data = stackalloc byte[fullText.Length];
		Encoding.ASCII.GetBytes(fullText, data);
		Span<byte> hash = stackalloc byte[8];
		Crc64.Hash(data, hash);
		return BitConverter.ToUInt64(hash);
	}
}