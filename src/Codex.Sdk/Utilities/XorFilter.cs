using System.Collections;
using System.Security.Cryptography;

namespace Codex.Utilities;

public delegate IArray<uint> Allocate(int length);

public record XorFilter(IArray<uint> Data)
{
    /// <summary>
    /// Xor filters always use 3 hash functions
    /// </summary>
    private const int SLOT_HASH_FUNCTION_COUNT = 3;

    /// <summary>
    /// We use hash index 3 fingerprint hash. [0-2] are used for table slot selection.
    /// </summary>
    private const int FINGERPRINT_HASH_INDEX = SLOT_HASH_FUNCTION_COUNT;


    //public XorFilter Build(IEnumerable<MurmurHash> itemHashes)
    //{
    //    var slotCounts = new byte[Data.Length];
    //}

    public static ulong GetHashValue(MurmurHash hash, int hashIndex)
    {
        return unchecked(hash.Low + (hash.High * (ulong)hashIndex));
    }

    public static uint GetFingerprintHash(MurmurHash hash)
    {
        return unchecked((uint)GetHashValue(hash, SLOT_HASH_FUNCTION_COUNT));
    }

    private record Builder(IArray<uint> Data, int[] SlotAssignments, MurmurHash[] ItemHashes)
    {
        // We start out skipping conflicts. If we reach a state where
        // no slots can be assigned due to conflicts. We switch to a
        // last one wins strategy.
        private bool OverwriteConflicts = false;

        public void Build()
        {
            int remainingAssigments = ItemHashes.Length;
            BitArray assignedItems = new BitArray(ItemHashes.Length);
            while (remainingAssigments > 0)
            {
                for (int i = 0; i < remainingAssigments; i++)
                {
                    // Skip if item is already assigned
                    if (assignedItems[i]) continue;

                    var item = ItemHashes[i];
                    Add(i, item);
                }

                int newlyAssignedItemCount = 0;
                for (int i = 0; i < SlotAssignments.Length; i++)
                {
                    ref var slot = ref SlotAssignments[i];
                    if (slot < 0)
                    {
                        if (slot == int.MinValue)
                        {
                            // Slot had conflict. Clear for next add cycle.
                            slot = 0;
                        }

                        // Slot has assigned item from prior iteration
                        // or slot has a conflict.
                        continue;
                    }

                    if (slot > 0)
                    {
                        var itemIndex = slot - 1;
                        if (assignedItems[itemIndex])
                        {
                            // item is already assigned. Clear the slot.
                            slot = 0;
                        }
                        else
                        {
                            remainingAssigments--;
                            newlyAssignedItemCount++;
                            // Set slot to negative value indicating that is is
                            // reserved
                            slot = -slot;
                            assignedItems[itemIndex] = true;
                        }
                    }
                }

                Contract.Assert(newlyAssignedItemCount != 0);
                if (newlyAssignedItemCount == 0)
                {
                    if (!OverwriteConflicts)
                    {
                        OverwriteConflicts = true;
                    }
                    // We have a problem.
                }
            }
        }

        public void Add(int itemIndex, MurmurHash hash)
        {
            // Zero means, the slot is empty so we add one to the index
            itemIndex++;

            for (int i = 0; i < SLOT_HASH_FUNCTION_COUNT; i++)
            {
                int slotIndex = (int)(GetHashValue(hash, i) % (ulong)SlotAssignments.Length);
                ref var slot = ref SlotAssignments[slotIndex];
                if (slot == 0 || (slot > 0 && OverwriteConflicts))
                {
                    slot = itemIndex;
                }
                else if (slot > 0)
                {
                    // Set to min value indicating the slot has a conflict
                    slot = int.MinValue;
                }
            }
        }
    }

}