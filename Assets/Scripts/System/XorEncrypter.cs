using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class XorEncrypter
{
	public static byte[] Encrypt(string text, string key)
	{
		var bytes = System.Text.Encoding.UTF8.GetBytes(text);
		if (!string.IsNullOrEmpty(key))
		{
			bytes = Xor(bytes, key);
		}
		return bytes;
	}

	public static string Decrypt(byte[] bytes, string key)
	{
		if (!string.IsNullOrEmpty(key))
		{
			bytes = Xor(bytes, key);
		}
		var text = System.Text.Encoding.UTF8.GetString(bytes);
		return text;
	}

	// non public ------
	static byte[] Xor(byte[] bytes, string key)
	{
		var ret = new byte[bytes.Length];
		var keyPos = 0;
		for (var i = 0; i < bytes.Length; i++)
		{
			ret[i] = (byte)(bytes[i] ^ (key[keyPos] & 0xff));
			keyPos++;
			if (keyPos >= key.Length)
			{
				keyPos = 0;
			}
		}
		return ret;
	}
}
