using System;

namespace BrainFuck
{
    public class BrainFuckBack
    {
        public const short RANGE = 30000;
        private byte[] values = new byte[RANGE];
        private short ptr = 0;

        public BrainFuckBack()
        {
        }

        public void Next(short amount = 1)
        {
            ptr += amount;
            if (ptr >= RANGE)
                ptr -= RANGE;
            OnPtrChanged?.Invoke(this, new());
        }

        public void Prev(short amount = 1)
        {
            if (ptr <= 0)
                ptr += RANGE;
            ptr -= amount;
            OnPtrChanged?.Invoke(this, new());
        }

        public void Add(byte amount = 1)
        {
            values[ptr] += amount;
            OnValueChanged?.Invoke(this, new());
        }

        public void Sub(byte amount = 1)
        {
            values[ptr] -= amount;
            OnValueChanged?.Invoke(this, new());
        }

        public void Set(params byte[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                this.values[(ptr + i) % RANGE] = values[i];
            }
            OnValueChanged?.Invoke(this, new());
        }

        public byte this[short index] => values[index];
        public short Ptr => ptr;

        public event EventHandler? OnValueChanged;
        public event EventHandler? OnPtrChanged;
    }
}
