using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Codex.Lucene.Utilities
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    public interface IBitSet : IBits
    {
        int NextSetBit(int minValue);
    }

    /// <summary>
    /// Simple <see cref="DocIdSet"/> and <see cref="DocIdSetIterator"/> backed by a <see cref="IBitSet"/> 
    /// </summary>
    public class BitSetDocIdSet : DocIdSet, IBits
    {
        private readonly IBitSet bitSet;

        public BitSetDocIdSet(IBitSet bitSet)
        {
            this.bitSet = bitSet;
        }

        public override DocIdSetIterator GetIterator()
        {
            return new BitSetDocIdSetIterator(bitSet);
        }

        public override IBits Bits => this;

        /// <summary>
        /// This DocIdSet implementation is cacheable. </summary>
        public override bool IsCacheable => true;

        /// <summary>
        /// Returns the underlying <see cref="IBitSet"/>.
        /// </summary>
        public virtual IBitSet IBitSet => this.bitSet;

        public bool Get(int index)
        {
            return bitSet.Get(index);
        }

        public int Length =>
            // the size may not be correct...
            bitSet.Length;

        private class BitSetDocIdSetIterator : DocIdSetIterator
        {
            private int docId;
            private readonly IBitSet bitSet;

            internal BitSetDocIdSetIterator(IBitSet bitSet)
            {
                this.bitSet = bitSet;
                this.docId = -1;
            }

            public override int DocID => docId;

            public override int NextDoc()
            {
                // (docId + 1) on next line requires -1 initial value for docNr:
                var d = bitSet.NextSetBit(docId + 1);
                // -1 returned by IBitSet.nextSetBit() when exhausted
                docId = d == -1 ? NO_MORE_DOCS : d;
                return docId;
            }

            public override int Advance(int target)
            {
                int d = bitSet.NextSetBit(target);
                // -1 returned by IBitSet.nextSetBit() when exhausted
                docId = d == -1 ? NO_MORE_DOCS : d;
                return docId;
            }

            public override long GetCost()
            {
                // upper bound
                return bitSet.Length;
            }
        }
    }
}