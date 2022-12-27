using System;

namespace BrainFuck
{
    public class BrainFuckBack
    {
        public const short RANGE = 30_000;
        private byte[] values = new byte[RANGE];

        public byte this[short index] => values[index];
        public short Ptr { get; private set; } = 0;

        public event EventHandler? OnValueChanged;
        public event EventHandler? OnPtrChanged;

        public BrainFuckBack() { }

        public void Next(short amount = 1)
        {
            Ptr += amount;
            if (Ptr >= RANGE)
                Ptr -= RANGE;
            OnPtrChanged?.Invoke(this, new());
        }

        public void Prev(short amount = 1)
        {
            if (Ptr <= 0)
                Ptr += RANGE;
            Ptr -= amount;
            OnPtrChanged?.Invoke(this, new());
        }

        public void Add(byte amount = 1)
        {
            values[Ptr] += amount;
            OnValueChanged?.Invoke(this, new());
        }

        public void Sub(byte amount = 1)
        {
            values[Ptr] -= amount;
            OnValueChanged?.Invoke(this, new());
        }

        public void Set(params byte[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                this.values[(Ptr + i) % RANGE] = values[i];
            }
            OnValueChanged?.Invoke(this, new());
        }
    }
}
