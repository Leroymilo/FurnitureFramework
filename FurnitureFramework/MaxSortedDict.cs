// A custom Dictionary that tracks the max key for faster requests.

class MaxDict<TValue> : Dictionary<int, TValue>
{
	int max_key = int.MinValue;

	new public void Add(int key, TValue value)
	{
		if (key > max_key)
			max_key = key;
		
		base.Add(key, value);
	}

	new public void Remove(int key)
	{
		base.Remove(key);

		if (key == max_key)
			max_key = Keys.Max();
	}

    /// <summary>
    /// Method <c>LastValue</c> returns the value associated with the largest key.
    /// </summary>
	public TValue LastValue()
	{
		return this[max_key];
	}
}