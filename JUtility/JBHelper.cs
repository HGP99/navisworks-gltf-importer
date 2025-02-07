﻿/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BUtility
{
    partial class BHelper
    {
        public static void PopulateJson(BNode _BNode, JObject _NodeJObject)
        {
            if (_BNode == null) return;

            if (_BNode.Metadata != null)
            {
                var MetadataJObject = new JObject();
                foreach (var CurrentField in _BNode.Metadata)
                {
                    MetadataJObject.Add(CurrentField.Key, CurrentField.Value);
                }
                _NodeJObject.Add("metadata", MetadataJObject);
            }

            if (_BNode.Children == null) return;

            var ChildrenJArray = new JArray();

            foreach (var CurrentChild in _BNode.Children)
            {
                var ChildNodeJObject = new JObject();

                PopulateJson(CurrentChild, ChildNodeJObject);

                ChildrenJArray.Add(ChildNodeJObject);
            }

            _NodeJObject.Add("children", ChildrenJArray);
        }

        public static BNode BuildTreeFromJson(JObject _NodeJObject)
        {
            var CurrentNode = new BNode();

            if (_NodeJObject.ContainsKey("metadata"))
            {
                var MJObject = (JObject)_NodeJObject["metadata"];

                CurrentNode.Metadata = new List<BMetadata>(MJObject.Count);

                foreach (var Entry in MJObject)
                {
                    CurrentNode.Metadata.Add(new BMetadata(Entry.Key, (string)Entry.Value));
                }
            }

            if (_NodeJObject.ContainsKey("children"))
            {
                var CJArray = (JArray)_NodeJObject["children"];

                CurrentNode.Children = new List<BNode>(CJArray.Count);

                foreach (JObject ChildNodeJObject in CJArray)
                {
                    var ChildNode = BuildTreeFromJson(ChildNodeJObject);
                    CurrentNode.Children.Add(ChildNode);
                }
            }

            return CurrentNode;
        }

        public static BNode BuildTreeFromGLTF(JArray _Nodes)
        {
            if (_Nodes == null || _Nodes.Count == 0) return null;

            var NodeMetadataList = new List<BMetadata>[_Nodes.Count];
            var NodeParentChildIndexMap = new int[_Nodes.Count][];
            var bHasParentList = new bool[_Nodes.Count];

            int i = 0;
            foreach (JObject NodeJObject in _Nodes)
            {
                int[] ChildrenIndexesArray = null;
                if (NodeJObject.TryGetValue("children", out JToken ChildrenToken))
                {
                    var Children = (JArray)ChildrenToken;
                    if (Children.Count > 0)
                    {
                        ChildrenIndexesArray = new int[Children.Count];

                        int j = 0;
                        foreach (int Child in Children)
                        {
                            ChildrenIndexesArray[j++] = Child;
                        }
                    }
                }
                NodeParentChildIndexMap[i] = ChildrenIndexesArray;

                List<BMetadata> NodeMetadata = null;
                if (NodeJObject.TryGetValue("extras", out JToken ExtrasToken))
                {
                    var Extras = (JObject)ExtrasToken;
                    if (Extras.Count > 0)
                    {
                        NodeMetadata = new List<BMetadata>(Extras.Count);

                        foreach (var Extra in Extras)
                        {
                            NodeMetadata.Add(new BMetadata(Extra.Key, (string)Extra.Value));
                        }
                    }
                }
                if (NodeMetadata == null)
                {
                    NodeMetadata = new List<BMetadata>();
                }

                NodeMetadataList[i] = NodeMetadata;

                i++;
            }

            var TmpNodeMap = new BNode[_Nodes.Count];

            i = 0;
            foreach (var ChildrenIndexesArray in NodeParentChildIndexMap)
            {
                BNode CurrentNode;

                if (TmpNodeMap[i] != null)
                {
                    CurrentNode = TmpNodeMap[i];
                }
                else
                {
                    CurrentNode = new BNode();
                    TmpNodeMap[i] = CurrentNode;
                }

                CurrentNode.GLTFNodeIndex = i;

                if (NodeMetadataList[i] != null)
                {
                    CurrentNode.Metadata = NodeMetadataList[i];
                }

                i++;
                //Do not use i anymore!

                if (ChildrenIndexesArray == null)
                {
                    CurrentNode.Children = new List<BNode>();
                    continue; //Does not have child
                }

                CurrentNode.Children = new List<BNode>(ChildrenIndexesArray.Length);

                foreach (var NodeChildIndex in ChildrenIndexesArray)
                {
                    BNode ChildNode;

                    if (TmpNodeMap[NodeChildIndex] != null)
                    {
                        ChildNode = TmpNodeMap[NodeChildIndex];
                    }
                    else
                    {
                        ChildNode = new BNode();
                        TmpNodeMap[NodeChildIndex] = ChildNode;
                    }

                    ChildNode.GLTFNodeIndex = NodeChildIndex;
                    bHasParentList[NodeChildIndex] = true;

                    CurrentNode.Children.Add(ChildNode);
                }
            }

            BNode RootNode = null;
            foreach (var CurrentNode in TmpNodeMap)
            {
                if (!bHasParentList[CurrentNode.GLTFNodeIndex])
                {
                    RootNode = CurrentNode;
                    break;
                }
            }

            return RootNode;
        }
    }
}