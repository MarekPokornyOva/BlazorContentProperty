#region using
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
#endregion using

namespace Microsoft.AspNetCore.Components
{
	public class ContentsComponentBase : ComponentBase
	{
		public override Task SetParametersAsync(ParameterView parameters)
		{
			ReplaceChildContent(ref parameters);
			return base.SetParametersAsync(parameters);
		}

		void ReplaceChildContent(ref ParameterView parameters)
		{
			RenderTreeFrame[] frames = ParameterCollectionHelper.GetFrames(parameters);
			int lastContentBlazorComponentPos = -1;
			int pos = 0;
			foreach (RenderTreeFrame frame in frames)
			{
				if (frame.FrameType == RenderTreeFrameType.Component)
					lastContentBlazorComponentPos = typeof(ContentsComponentBase).IsAssignableFrom(frame.ComponentType) ? pos : -1;
				else if ((lastContentBlazorComponentPos != -1) //check we have right component type
					&& (frame.FrameType == RenderTreeFrameType.Attribute) && (frame.AttributeName == RenderTreeBuilderHelper.ChildContent)  //we have to replace ChildContent only...
					&& (frame.AttributeValue is RenderFragment childContent) //...of right (RenderFragment) type...
					&& (pos > lastContentBlazorComponentPos) && (pos < lastContentBlazorComponentPos + frames[lastContentBlazorComponentPos].ComponentSubtreeLength)) //...and within the component scope
				{
					List<RenderTreeFrame> framesEdit = new List<RenderTreeFrame>(frames);

					//setup new rendering for childContent
					RenderTreeBuilder rtb = new RenderTreeBuilder();
					childContent(rtb);
					RenderTreeFrame[] newFrames = ParseFragments(rtb, frames[pos].Sequence);

					//tune component length
					RenderTreeFrame fe = framesEdit[lastContentBlazorComponentPos];
					RenderTreeFrameHelper.SetComponentSubtreeLength(ref fe, framesEdit[lastContentBlazorComponentPos].ComponentSubtreeLength + newFrames.Length - 1);
					framesEdit[lastContentBlazorComponentPos] = fe;

					//replace ChildContent frame with contents
					framesEdit.RemoveAt(pos);
					framesEdit.InsertRange(pos, newFrames);

					ParameterCollectionHelper.SetFrames(ref parameters, framesEdit.ToArray());
					break;
				}
				pos++;
			}
		}

		private RenderTreeFrame[] ParseFragments(RenderTreeBuilder builder, int sequence)
		{
			ArrayRange<RenderTreeFrame> frames = builder.GetFrames();
			List<Tuple<string, RenderFragment>> parms = new List<Tuple<string, RenderFragment>>();
			for (int a = 0; a < frames.Count; a++)
			{
				RenderTreeFrame item = frames.Array[a];

				if ((item.FrameType == RenderTreeFrameType.Component) && (item.ComponentType.Equals(typeof(ContentProperty))))
				{
					RenderTreeFrame item1 = frames.Array[a + 1];
					if ((item1.FrameType == RenderTreeFrameType.Attribute) && (item1.AttributeName == "Name") && (item1.AttributeValue is string propertyName))
					{
						RenderTreeFrame item2 = frames.Array[a + 2];
						if ((item2.FrameType == RenderTreeFrameType.Attribute) && (item2.AttributeName == RenderTreeBuilderHelper.ChildContent) && (item2.AttributeValue is RenderFragment splitPartChildContent))
							parms.Add(new Tuple<string, RenderFragment>(propertyName, splitPartChildContent));
					}
					a += 2;
				}
			}

			RenderTreeFrame[] result = new RenderTreeFrame[parms.Count];
			int b = 0;
			foreach (Tuple<string, RenderFragment> item in parms)
			{
				result[b] = RenderTreeFrameHelper.Attribute(sequence + b, item.Item1, item.Item2);
				b++;
			}
			return result;
		}

		static class RenderTreeBuilderHelper
		{
			internal readonly static string ChildContent;
			internal const string ChildContentBackup = "ChildContent";
			static RenderTreeBuilderHelper()
			{
				FieldInfo fi = typeof(RenderTreeBuilder).GetField("ChildContent",BindingFlags.NonPublic|BindingFlags.Static);
				ChildContent=(fi?.GetRawConstantValue() as string)??ChildContentBackup;
			}
		}

		static class ParameterCollectionHelper
		{
			static FieldInfo _framesField = typeof(ParameterView).GetField("_frames", BindingFlags.NonPublic | BindingFlags.Instance);

			internal static RenderTreeFrame[] GetFrames(ParameterView parameters)
				=> (RenderTreeFrame[])_framesField.GetValue(parameters);

			internal static void SetFrames(ref ParameterView parameters, RenderTreeFrame[] frames)
			{
				object o = parameters;
				_framesField.SetValue(o, frames);
				parameters = (ParameterView)o;
			}
		}

		class RenderTreeFrameHelper
		{
			static MethodInfo _attribute;
			static FieldInfo _componentSubtreeLength;
			static RenderTreeFrameHelper()
			{
				Type t = typeof(RenderTreeFrame);
				_attribute = t.GetMethod("Attribute", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(int), typeof(string), typeof(object) }, null);
				_componentSubtreeLength = t.GetField("ComponentSubtreeLength");
			}

			internal static RenderTreeFrame Attribute(int sequence, string name, object value)
				=> (RenderTreeFrame)_attribute.Invoke(null, new object[] { sequence, name, value });

			internal static void SetComponentSubtreeLength(ref RenderTreeFrame frame, int value)
			{
				object o = frame;
				_componentSubtreeLength.SetValue(o, value);
				frame = (RenderTreeFrame)o;
			}
		}
	}

	public class ContentProperty : ComponentBase
	{
		[Parameter]
		public string Name { get; set; } //this is not needed in runtime but looks better in razor view

		/*
		All the component is handled by ContentsBlazorComponent so the following is not used

		[Parameter]
		public RenderFragment ChildContent { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			base.BuildRenderTree(builder);
			builder.AddContent(0, ChildContent);
		}*/
    }
}
