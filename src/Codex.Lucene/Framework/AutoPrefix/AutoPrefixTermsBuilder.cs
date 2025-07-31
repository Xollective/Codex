using Lucene.Net.Index;
using System.Diagnostics;

namespace Codex.Lucene.Framework.AutoPrefix
{
    public class AutoPrefixTermsBuilder<T>(T rootValue, Action<AutoPrefixTermNode<T>> onPersist)
        where T : INodeValue<T>
    {
        private readonly Action<AutoPrefixTermNode<T>> OnPersist = onPersist;

        public AutoPrefixTermNode<T> CurrentNode { get; private set; } = new(rootValue, null);

        public AutoPrefixTermNode<T> StartTerm(BytesRefString text)
        {
            Print("Start", text);
            Print("StartNode", CurrentNode);
            BytesRefString commonPrefix = CurrentNode.GetCommonPrefix(text);
            Print("CommonPrefix", commonPrefix);

            while (commonPrefix.Length < CurrentNode.Term.Length)
            {
                var priorNodeLength = CurrentNode.Prior?.Term.Length;
                if (commonPrefix.Length > priorNodeLength)
                {
                    PersistNode();
                    CurrentNode.Term = commonPrefix;
                    break;
                }

                // Common prefix is shorter than the current prefix so we need to
                // persist and pop the current node
                PopNode();
            }

            Print("BeforePush", CurrentNode);
            CurrentNode = CurrentNode.Push(text);
            Print("AfterPush", CurrentNode);
            return CurrentNode;
        }

        public void Finish()
        {
            while (CurrentNode.Term.Length > 0)
            {
                PopNode();
            }
        }

        protected void PersistNode()
        {
            Print("Persist", CurrentNode);
            OnPersist(CurrentNode);
        }

        private void PopNode()
        {
            PersistNode();
            Print("BeforePop", CurrentNode);
            CurrentNode.Pop();
            CurrentNode = CurrentNode.Prior;
            Print("AfterPop", CurrentNode);
        }

        [Conditional("DEBUG2")]
        private void Print(string message, AutoPrefixTermNode<T> node)
        {
            Print($"{message} #:{node?.Height}", node?.Term);
        }

        [Conditional("DEBUG2")]
        public void Print(string message, BytesRefString? term)
        {
            System.Diagnostics.Debug.WriteLine($"{message} '{term}'");
            Console.WriteLine($"{message.PadRight(20, ' ')} '{term}'");
        }
    }
}
