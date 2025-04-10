namespace PowerTree
{
// Helper class for node counters
    public class Counter
    {
        public int Value { get; private set; }
        public int MaxValue { get; set; }
        
        public event System.Action<int> OnValueChanged;
        public event System.Action OnMaxValueReached;
        
        public Counter(int maxValue = int.MaxValue)
        {
            Value = 0;
            MaxValue = maxValue;
        }
        
        public void Increment()
        {
            Value++;
            OnValueChanged?.Invoke(Value);
            
            if (Value >= MaxValue)
            {
                OnMaxValueReached?.Invoke();
            }
        }
        
        public void Decrement()
        {
            if (Value > 0)
            {
                Value--;
                OnValueChanged?.Invoke(Value);
            }
        }
        
        public void Reset()
        {
            Value = 0;
            OnValueChanged?.Invoke(Value);
        }
        
        public void SetValue(int value)
        {
            Value = value;
            OnValueChanged?.Invoke(Value);
            
            if (Value >= MaxValue)
            {
                OnMaxValueReached?.Invoke();
            }
        }
    }
}