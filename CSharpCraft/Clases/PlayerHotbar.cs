using CSharpCraft.Clases.Item;
using System;
using System.Collections.Generic;

namespace CSharpCraft.Classes
{
    public class PlayerHotbar
    {
        private const int HotbarSize = 9;
        private List<Item> _items;
        public int SelectedIndex { get; private set; } = 0;
        public Item SelectedItem { get; private set; }

        public PlayerHotbar()
        {
            _items = new List<Item>(HotbarSize);
            Initialize();
        }

        public void Initialize()
        {
            for (int i = 0; i < HotbarSize; i++)
            {
                _items.Add(new EmptyItem());
            }
            SelectedIndex = 0;
            SelectedItem = _items[SelectedIndex];
        }

        public Item SelectItem(int index)
        {
            if (index < 0 || index >= HotbarSize)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the hotbar.");
            }
            SelectedIndex = index;
            SelectedItem = _items[SelectedIndex];
            return _items[index];
        }

        public void SetItem(int index, Item item)
        {
            if (index < 0 || index >= HotbarSize)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the hotbar.");
            }
            _items[index] = item;
        }

        public List<Item> GetAllItems()
        {
            // Return a copy of the list to prevent external modification
            return new List<Item>(_items);
        }

        // New method to handle key presses 1-9
        public Item SelectItemByKey(int key)
        {
            if (key < 1 || key > HotbarSize)
            {
                throw new ArgumentOutOfRangeException(nameof(key), "Key must be between 1 and 9.");
            }
            return SelectItem(key - 1);
        }
    }
}
