using System;
using System.Collections.Generic;
// Migrated from Newtonsoft.Json to SimpleJson

namespace UnityMcp
{
    public class StateTreeBuilder
    {
        private readonly StateTree root;
        private readonly Stack<StateTree> nodeStack = new();

        private StateTreeBuilder()
        {
            root = new StateTree();
            nodeStack.Push(root);
        }

        public static StateTreeBuilder Create()
        {
            return new StateTreeBuilder();
        }

        private StateTree Current => nodeStack.Peek();

        public StateTreeBuilder Key(string variableKey)
        {
            Current.key = variableKey;
            return this;
        }

        public StateTreeBuilder Branch(object edgeKey)
        {
            // Preventnull keyCause anomaly
            if (edgeKey == null)
            {
                throw new ArgumentNullException(nameof(edgeKey), "edgeKey cannot be null in Branch method");
            }

            if (!Current.select.TryGetValue(edgeKey, out var child))
            {
                child = new StateTree();
                Current.select[edgeKey] = child;
            }
            nodeStack.Push(child);
            return this;
        }

        public StateTreeBuilder DefaultBranch()
        {
            return Branch(StateTree.Default);
        }
        public StateTreeBuilder Leaf(object edgeKey, Func<JsonClass, object> action)
        {
            // Preventnull keyCause anomaly
            if (edgeKey == null)
            {
                throw new ArgumentNullException(nameof(edgeKey), "edgeKey cannot be null in Leaf method");
            }

            Current.select[edgeKey] = (StateTree)action;
            return this;
        }
        public StateTreeBuilder Leaf(object edgeKey, Func<StateTreeContext, object> action)
        {
            // Preventnull keyCause anomaly
            if (edgeKey == null)
            {
                throw new ArgumentNullException(nameof(edgeKey), "edgeKey cannot be null in Leaf method");
            }

            Current.select[edgeKey] = (StateTree)action;
            return this;
        }

        public StateTreeBuilder DefaultLeaf(Func<StateTreeContext, object> action)
        {
            return Leaf(StateTree.Default, action);
        }

        public StateTreeBuilder DefaultLeaf(Func<JsonClass, object> action)
        {
            return Leaf(StateTree.Default, action);
        }

        /// <summary>
        /// Add optional parameter branch：Execute corresponding action when the specified parameter exists
        /// </summary>
        /// <param name="parameterName">Parameter name to check</param>
        /// <param name="action">Action to execute when parameter exists</param>
        public StateTreeBuilder OptionalLeaf(string parameterName, Func<JsonClass, object> action)
        {
            // Directly use parameter name askey，And add to optional parameter collection
            Current.select[parameterName] = (StateTree)action;
            Current.optionalParams.Add(parameterName);
            return this;
        }
        /// <summary>
        /// Add optional parameter branch：Execute corresponding action when the specified parameter exists
        /// </summary>
        /// <param name="parameterName">Parameter name to check</param>
        /// <param name="action">Action to execute when parameter exists</param>
        public StateTreeBuilder OptionalLeaf(string parameterName, Func<StateTreeContext, object> action)
        {
            // Directly use parameter name askey，And add to optional parameter collection
            Current.select[parameterName] = (StateTree)action;
            Current.optionalParams.Add(parameterName);
            return this;
        }
        /// <summary>
        /// Add optional parameter branch：Enter subbranch when specified parameter exists
        /// </summary>
        /// <param name="parameterName">Parameter name to check</param>
        public StateTreeBuilder OptionalBranch(string parameterName)
        {
            // Directly use parameter name askey，And add to optional parameter collection
            Current.optionalParams.Add(parameterName);
            return Branch(parameterName);
        }

        /// <summary>
        /// Add optional parameter node：Enter subbranch and set when specified parameter existskey
        /// Equivalent to OptionalBranch + Key Combination of
        /// </summary>
        /// <param name="parameterName">Parameter name to check</param>
        /// <param name="variableKey">Variable of subbranchkey</param>
        public StateTreeBuilder OptionalNode(string parameterName, string variableKey)
        {
            return OptionalBranch(parameterName).Key(variableKey);
        }

        /// <summary>
        /// Add optional parameter branch：Enter subbranch and set when specified parameter existskey
        /// </summary>
        /// <param name="parameterName">Parameter name to check</param>
        /// <param name="variableKey">Variable of subbranchkey</param>
        public StateTreeBuilder OptionalKey(string parameterName)
        {
            return OptionalBranch(parameterName).Key(parameterName);
        }

        public StateTreeBuilder Node(object edgeKey, string variableKey)
        {
            return Branch(edgeKey).Key(variableKey);
        }

        public StateTreeBuilder NodeNext(string edgeKey)
        {
            return Branch(edgeKey).Key("");
        }

        public StateTreeBuilder Up()
        {
            if (nodeStack.Count > 1)
            {
                nodeStack.Pop();
            }
            return this;
        }

        public StateTreeBuilder ULeaf(object edgeKey, Func<StateTreeContext, object> action)
        {
            return Up().Leaf(edgeKey, action);
        }

        public StateTreeBuilder ULeaf(object edgeKey, Func<JsonClass, object> action)
        {
            return Up().Leaf(edgeKey, action);
        }


        public StateTreeBuilder UNode(object edgeKey, string variableKey)
        {
            return Up().Node(edgeKey, variableKey);
        }

        public StateTree Build()
        {
            return root;
        }
    }
}


