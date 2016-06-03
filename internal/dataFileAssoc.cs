using UnityEngine;
using System.Collections;
using System.Globalization;

//this utilizes a text file to save/load data arranged as an associative array

[System.Serializable]
public class keyPair
{
    public keyPair(string _key, string _val) { key = _key; value = _val; }
    public string key;
    public string value;
}

public class dataFileAssoc : MonoBehaviour {

    public string fileName;
    public bool readOnly = false;
    public bool loadOnAwake = false;
    public keyPair[] keyPairs = new keyPair[0];

    void Awake()
    {
        if (loadOnAwake)
            load();
    }

    public void clear()
    {
        keyPairs = new keyPair[0];
    }


    public bool hasKey(string _key)
    {
        int i;
        for (i = 0; i < keyPairs.Length; i++)
        {
            if (keyPairs[i].key == _key)
                return true;
        }
        return false;
    }

    //do any of the keys contain the given value?
    //returns the first array element number that does
    //returns -1 if none do.
    public int hasValue(string _val)
    {
        int i;
        for (i = 0; i < keyPairs.Length; i++)
        {
            if (keyPairs[i].value == _val)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Same as setValue()
    /// </summary>
    /// <param name="_key">the associative key</param>
    /// <param name="_val">the value you want the key to have</param>
    /// <returns>returns true if it set the value of the key, returns false if it added the key.</returns>
    public bool addKey(string _key, string _val) 
    {
       return setValue( _key,  _val, true);
    }


    /// <summary>
    /// Set the value for an existing key.
    /// </summary>
    /// <param name="_key">the associative key</param>
    /// <param name="_val">the value you want the key to have</param>
    /// <returns>returns true if it set the value of the key, returns false if it added the key.</returns>
    public bool setValue(string _key, string _val) 
    {
        return setValue(_key, _val, true);
    }
    public bool setValue(string _key, string _val, bool addIfMissing) //more intuitively named override
    {
        if (readOnly)
        {
            Debug.LogWarning("WARNING: Tried to set a value on the READ ONLY dataFileAssoc component in: " + this.name + ". Ignoring.");
            return false;
        }

        if (!addIfMissing)
            return internalSetValue(_key, _val);

        //add it if missing

        if (hasKey(_key)) 
            return internalSetValue(_key, _val);

        System.Array.Resize(ref keyPairs, keyPairs.Length + 1);
        keyPairs[keyPairs.Length - 1] = new keyPair(_key, _val);
        return true;
    }
    protected bool internalSetValue(string _key, string _val) //internal version of setValue() ...so that we can still set the data file values from the file despite readOnly
    {
        int i;
        for (i = 0; i < keyPairs.Length; i++)
        {
            if (keyPairs[i].key == _key)
            {
                keyPairs[i].value = _val;
                return true;
            }
        }
        return false;  //if it fails we do not add an element, simply ignore and return false
    }


    public string getValue(string _key)  //returns an empty string if it can't match the key
    {
        return getValue(_key, "");
    }

    public string getValue(string _key, string defaultValue)  //will return defaultValue if it can't match the _key
    {
        int i;
        for (i = 0; i < keyPairs.Length; i++)
        {
            if (keyPairs[i].key == _key)
                return keyPairs[i].value;
        }
        return defaultValue;
    }


    public string getValue(int index)   //returns an empty string if index is out of range
    {
        if (index >= keyPairs.Length)
            return "";

        return keyPairs[index].value;
    }


    public int getValueAsInt(string _key, int defaultValue)  //will return defaultValue if it can't match the _key, or if the data can't be converted to an int
    {
        int i;
        for (i = 0; i < keyPairs.Length; i++)
        {
            if (keyPairs[i].key == _key)
            {
                return stringToInt(keyPairs[i].value, defaultValue);
            }
        }
        return defaultValue;  //could not match key to any entry.
    }


    public long getValueAsLong(string _key, int defaultValue)  //will return defaultValue if it can't match the _key, or if the data can't be converted to an int
    {
        int i;
        for (i = 0; i < keyPairs.Length; i++)
        {
            if (keyPairs[i].key == _key)
            {
                return stringToLong(keyPairs[i].value, defaultValue);
            }
        }
        return defaultValue;  //could not match key to any entry.
    }


    public float getValueAsFloat(string _key, float defaultValue)  //will return defaultValue if it can't match the _key
    {
        int i;
        for (i = 0; i < keyPairs.Length; i++)
        {
            if (keyPairs[i].key == _key)
            {
                return stringToFloat(keyPairs[i].value, defaultValue);
            }
        }
        return defaultValue;  //could not match key to any entry.
    }


    public bool getValueAsBool(string _key, bool defaultValue)
    {
        int i;
        for (i = 0; i < keyPairs.Length; i++)
        {
            if (keyPairs[i].key == _key)
            {
                return stringToBool(keyPairs[i].value, defaultValue);
            }
        }
        return defaultValue;  //could not match key to any entry.
    }



    /// <summary>
    /// load values from a file on the disk  
    /// </summary>
    /// <param name="populate">If true, adds  keys found in the data file that don't already exist in the keyPair list. If false, ignores unknown keys in the data file.</param>
    /// <returns>true on success, false on failure</returns>
    public virtual bool load(bool populate = true) 
    {
        if (fileName == "")
        {
            Debug.Log("Tried to load from dataFile component in: " + this.name + ", but the fileName has not been set.");
            return false;
        }

        if (System.IO.File.Exists(fileName))
        {
            try
            {                 
                // Read the file and display it line by line.
                System.IO.StreamReader file = new System.IO.StreamReader(fileName);
                string line =  file.ReadLine();
                while(line != null && !line.StartsWith("#"))
                {
                    //   Debug.Log(line);
                        string[] kp = line.Split('=');
                        if (kp.Length >= 2)
                        {
                            //handle lines like this: key = myAwesome=Value  otherwise this can break if it's reading in a link.
                            if (kp.Length > 2) 
                            {
                                for (int i = 2; i < kp.Length; i++) //stick all the extra stuff back into kp[1]
                                {
                                    kp[1] += "=" + kp[i];
                                }
                            }

                            kp[0] = kp[0].Trim(); //trim trailing whitespaces for safety
                            kp[1] = kp[1].Trim();

                            if (populate && !hasKey(kp[0])) //populate tells us to ADD non-existent keys, and this one doesn't exist so add it.
                            {
                                System.Array.Resize(ref keyPairs, keyPairs.Length + 1);
                                keyPairs[keyPairs.Length - 1] = new keyPair(kp[0], kp[1]);
                            }
                            else
                                internalSetValue(kp[0], kp[1]);  //either we are ignoring non-existent keys (!populate) or we already know that the key exists (from hasKey()).
                        }
                        else if (line == "")
                        {
                            //skip
                        }
                        else
                            Debug.Log("WARNING: invalid line in data file: " + fileName + " LINE: " + line);

                        line = file.ReadLine();
                }

                file.Close();
                return true;
            }
            catch
            {
                Debug.Log("The data file: " + fileName + " could not be read.");
                return false;
            }
        }
        Debug.Log("The data file: " + fileName + " does not exist, and therefore could not be read.");
        return false;
    }

    public virtual void save() //save to disk, note that comments are lost
    {
        if (fileName == "")
        {
            Debug.Log("Tried to save dataFileAssoc component in: " + this.name + ", but the fileName has not been set.");
            return;
        }
        else if (readOnly)
        {
            Debug.Log("WARNING: Tried to save dataFileAssoc component in: " + this.name + ", but it is set to readOnly. Ignoring the save() call.");
            return;
        }

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@fileName))
        {
            string lineText = "";
            int i;
            for (i = 0; i < keyPairs.Length; i++)
            {
                lineText = keyPairs[i].key + "=" + keyPairs[i].value;
                file.WriteLine(lineText);                  
            }
        }
    }


    //utilities
    public static bool stringToBool(string strVal, bool defaultVal)
    {
        if (strVal == "1" || strVal == "true" || strVal == "True" || strVal == "yes" || strVal == "on")
            return true;
        else if (strVal == "0" || strVal == "false" || strVal == "False" || strVal == "no" || strVal == "off")
            return false;
        else
            return defaultVal;
    }
    public static int stringToInt(string strVal, int defaultVal)
    {
        int output;
        if (System.Int32.TryParse(strVal, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out output))
            return output;

        return defaultVal;
    }
    public static long stringToLong(string strVal, long defaultVal)
    {
        long output;
        if (System.Int64.TryParse(strVal, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out output))
            return output;

        return defaultVal;
    }
    public static float stringToFloat(string strVal, float defaultVal)
    {
        float output;
        if (System.Single.TryParse(strVal, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out output))
            return output;

        return defaultVal;
    }


}
