using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

//this utilizes a text file to save/load data arranged as a dictionary
//the file should be arranged like so:
/*
 
myKey=itsValue
speed=10
 
 */

//use this component if you don't care about editing settings or defaults via the inspector, or want a more efficient algorithm
//Definitely use this instead of dataFileAssoc if you plan to have thousands of entries from the data file.


public class dataFileDict : MonoBehaviour {

    public string fileName;
    public bool readOnly = false; //disallows any changes to the data other than reading from a file. It also prevents saving, thereby protecting the contents of the file.
    public bool loadOnAwake = false;
    public Dictionary<string,string> keyPairs = new Dictionary<string,string>();

    void Awake()
    {
        if (loadOnAwake)
            load();
    }

    public void clear()
    {
        keyPairs.Clear();
    }


    public bool hasKey(string _key)
    {
        return keyPairs.ContainsKey(_key);
    }

    //do any of the keys contain the given value?
    public bool hasValue(string _val)
    {
        return keyPairs.ContainsValue(_val);
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

    public virtual bool setValue(string _key, int _val, bool addIfMissing = true)
    {
        return setValue(_key, _val.ToString(), addIfMissing);
    }
    public virtual bool setValue(string _key, short _val, bool addIfMissing = true)
    {
        return setValue(_key, _val.ToString(), addIfMissing);
    }
    public virtual bool setValue(string _key, float _val, bool addIfMissing = true)
    {
        return setValue(_key, _val.ToString(), addIfMissing);
    }
    public virtual bool setValue(string _key, bool _val, bool addIfMissing = true)
    {
        return setValue(_key, _val.ToString(), addIfMissing);
    }
    public virtual bool setValue(string _key, string _val, bool addIfMissing = true) //more intuitively named override
    {
        if (readOnly)
        {
            Debug.LogWarning("WARNING: Tried to set a value on the READ ONLY dataFileDict component in: " + this.name + ". Ignoring.");
            return false;
        }

        if (!addIfMissing)
            return internalSetValue(_key, _val);

        //add it if missing

        if (hasKey(_key)) 
            return internalSetValue(_key, _val);

        keyPairs.Add(_key, _val);
        return true;
    }
    protected bool internalSetValue(string _key, string _val) //internal version of setValue() ...so that we can still set the data file values from the file despite readOnly
    {
        if (!keyPairs.ContainsKey(_key))  //if it fails we do not add an element, simply ignore and return false
            return false;

        keyPairs[_key] = _val;
        return true;
    }


    public string getValue(string _key)  //returns an empty string if it can't match the key
    {
        return getValue(_key, "");
    }

    public string getValue(string _key, string defaultValue)  //will return defaultValue if it can't match the _key
    {
        if (!keyPairs.ContainsKey(_key))  //if it fails we do not add an element, simply ignore and return false
            return defaultValue;

        return keyPairs[_key];
    }

    public int getValueAsInt(string _key, int defaultValue)  //will return defaultValue if it can't match the _key, or if the data can't be converted to an int
    {

        if (!keyPairs.ContainsKey(_key))  //if it fails we do not add an element, simply ignore and return false
            return defaultValue;

        return stringToInt(keyPairs[_key], defaultValue);
    }


    public long getValueAsLong(string _key, int defaultValue)  //will return defaultValue if it can't match the _key, or if the data can't be converted to an int
    {
        if (!keyPairs.ContainsKey(_key))  //if it fails we do not add an element, simply ignore and return false
            return defaultValue;

        return stringToLong(keyPairs[_key], defaultValue);
    }


    public float getValueAsFloat(string _key, float defaultValue)  //will return defaultValue if it can't match the _key
    {
        if (!keyPairs.ContainsKey(_key))  //if it fails we do not add an element, simply ignore and return false
            return defaultValue;

        return stringToFloat(keyPairs[_key], defaultValue);
    }


    public bool getValueAsBool(string _key, bool defaultValue)
    {
        if (!keyPairs.ContainsKey(_key))  //if it fails we do not add an element, simply ignore and return false
            return defaultValue;

        return stringToBool(keyPairs[_key], defaultValue);
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
                string readText = System.IO.File.ReadAllText(fileName);
                return loadFromString(readText, populate);
            }
            catch
            {
                Debug.Log("The data file: " + fileName + " could not be read.");
                return false;
            }
        }
    //    Debug.Log("The data file: " + fileName + " does not exist, and therefore could not be read.");
        return false;
    }



    public virtual bool loadFromString(string str, bool populate = true)
    {
        bool foundAtLeastOneGoodSetting = false;           
        string[] lines = System.Text.RegularExpressions.Regex.Split(str, "\r\n|\r|\n");
        for (int l = 0; l < lines.Length; l++)
        {
            if (lines[l] == null || lines[l] == "" || lines[l].StartsWith("#"))
                continue;

            //   Debug.Log(line);
            string[] kp = lines[l].Split('=');
            if (kp.Length >= 2)
            {
                foundAtLeastOneGoodSetting = true;

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
                    keyPairs.Add(kp[0], kp[1]);
                }
                else
                    internalSetValue(kp[0], kp[1]);  //either we are ignoring non-existent keys (!populate) or we already know that the key exists (from hasKey()).
            }
            else
                Debug.Log("WARNING: invalid line in data file: " + fileName + " LINE: " + lines[l]);
        }

        return foundAtLeastOneGoodSetting;
    }

    public virtual bool save() //save to disk, note that comments are lost
    {
        if (fileName == "")
        {
            Debug.Log("Tried to save dataFileDict component in: " + this.name + ", but the fileName has not been set.");
            return false;
        }
        else if (readOnly)
        {
            Debug.Log("WARNING: Tried to save dataFileDict component in: " + this.name + ", but it is set to readOnly. Ignoring the save() call.");
            return false;
        }

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@fileName))
        {
            string a = getDataAsString();
            file.WriteLine(a);  
        }
        return true;
    }

    public virtual string getDataAsString()
    {
        string a = "";
        foreach (var pair in keyPairs)
        {
            a += pair.Key + "=" + pair.Value + "\n";
        }
        return a;
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

    public static string base64Encode(string plainText) 
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }
    public static string base64Decode(string base64EncodedData) 
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }


}
