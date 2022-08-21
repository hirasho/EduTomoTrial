using System.Reflection;
using UnityEngine;

public class Keys
{
	public virtual string VisionApiKey { get => null; }
	public virtual string VisionApiUnlockCommand { get => null; }
	public virtual string SaveDataEncryptionKey { get => null; }

	public static Keys Instantiate()
	{
		Keys ret = null;
		var baseType = typeof(Keys);
		var asssembly = Assembly.GetExecutingAssembly();
		var types = asssembly.GetTypes();

		System.Type foundType = null;
		foreach (var type in types)
		{
			if ((type != baseType) && baseType.IsAssignableFrom(type))
			{
				foundType = type;
				break;
			}
		}

		if (foundType == null)
		{
			foundType = baseType;
			Debug.LogError(baseType.Name + " の派生クラスが見当たりません。");
		}
		ret = System.Activator.CreateInstance(foundType) as Keys;
		return ret;
	}
}
