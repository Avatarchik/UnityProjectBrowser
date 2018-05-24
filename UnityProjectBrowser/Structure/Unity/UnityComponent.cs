﻿using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace ProjectBrowser
{
	public class UnityComponent : UnityObject
	{
		/// <summary>
		/// The type name of this object.
		/// </summary>
		public readonly string TypeName;

		private readonly UnityObjectKey m_scriptKey;

		private readonly UnityObjectKey m_gameObjectKey;

		/// <summary>
		/// Reads a <see cref="UnityComponent"/> from the specified Yaml node.
		/// </summary>
		public UnityComponent(string documentId, YamlDocument yaml, UnityObjectKey key)
			: base(documentId, yaml, key)
		{
			YamlMappingNode rootNode = (YamlMappingNode)yaml.RootNode;
			KeyValuePair<YamlNode, YamlNode> firstNode = rootNode.Children.First();
			YamlMappingNode objectNode = (YamlMappingNode)firstNode.Value;

			TypeName = ((YamlScalarNode)firstNode.Key).Value;

			foreach (KeyValuePair<YamlNode, YamlNode> kv in objectNode.Children)
			{
				YamlMappingNode valueMapping = kv.Value as YamlMappingNode;
				switch (((YamlScalarNode)kv.Key).Value)
				{
					case "m_GameObject":
						{
							m_gameObjectKey = ParseReference(valueMapping, key);
							AddRelationship(m_gameObjectKey, "is-component-of", "");
							break;
						}

					case "m_Script":
						{
							m_scriptKey = ParseReference(valueMapping, key);
							AddRelationship(m_scriptKey, "is-instance-of-script", "is-used-on-component");
							break;
						}

					case "m_Father":
						{
							UnityObjectKey reference = ParseReference(valueMapping, key);
							AddRelationship(reference, "has-transform-parent", "");
							break;
						}

					case "m_Children":
						{
							YamlSequenceNode childrenSequenceNode = (YamlSequenceNode)kv.Value;
							foreach (YamlNode child in childrenSequenceNode.Children)
							{
								YamlMappingNode childMappingNode = (YamlMappingNode)child;
								UnityObjectKey reference = ParseReference(childMappingNode, key);
								AddRelationship(reference, "is-transform-child-of", "");
							}
							break;
						}

					default:
						{
							// look through arbitrary data for object or asset references
							if (valueMapping != null)
							{
								foreach (YamlNode node in valueMapping.AllNodes)
								{
									YamlMappingNode mappingNode = node as YamlMappingNode;
									if (mappingNode != null)
									{
										UnityObjectKey reference = ParseReference(mappingNode, key);
										if (!reference.IsEmpty)
										{
											AddRelationship(reference, "has-reference-to", "is-referenced-by");
										}
									}
								}
							}
							break;
						}
				}
			}
		}

		public override string GetParentId()
		{
			if (!m_gameObjectKey.IsEmpty)
			{
				return m_gameObjectKey.ToString();
			}
			else
			{
				return base.GetParentId();
			}
		}

		public override string GetIconKey()
		{
			switch (TypeName)
			{
				case "MonoBehaviour":
					return "cs_Script_Icon";
				default:
					return TypeName + "_Icon";
			}
		}

		public override string ToString()
		{
			if (ObjectDatabase.TryGetObject(m_scriptKey, out ProjectObject scriptObject))
			{
				return scriptObject.ToString();
			}
			else
			{
				return TypeName;
			}
		}
	}
}
