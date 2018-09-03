#region using
//using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Collections.Generic;
using System.Reflection;
#endregion using

namespace Microsoft.AspNetCore.Blazor.Components
{
	public class ContentsBlazorComponent : BlazorComponent
	{
		public override void SetParameters(ParameterCollection parameters)
		{
			ReplaceChildContent(ref parameters);
			base.SetParameters(parameters);
		}

		void ReplaceChildContent(ref ParameterCollection parameters)
		{
			RenderTreeFrame[] frames = ParameterCollectionHelper.GetFrames(parameters);
			int lastContentBlazorComponentPos = -1;
			int pos = 0;
			foreach (RenderTreeFrame frame in frames)
			{
				if (frame.FrameType == RenderTreeFrameType.Component)
					lastContentBlazorComponentPos = typeof(ContentsBlazorComponent).IsAssignableFrom(frame.ComponentType) ? pos : -1;
				else if ((lastContentBlazorComponentPos != -1) //check we have right component type
					&& (frame.FrameType == RenderTreeFrameType.Attribute) && (frame.AttributeName == RenderTreeBuilder.ChildContent)  //we have to replace ChildContent only...
					&& (frame.AttributeValue is RenderFragment childContent) //...of right (RenderFragment) type...
					&& (pos > lastContentBlazorComponentPos) && (pos < lastContentBlazorComponentPos + frames[lastContentBlazorComponentPos].ComponentSubtreeLength)) //...and within the component scope
				{
					List<RenderTreeFrame> framesEdit = new List<RenderTreeFrame>(frames);

					//setup new rendering for childContent
					//there's possible issue with service provider used in renderer. I haven't found proper way to get right one.
					RenderTreeBuilder rtb = new RenderTreeBuilder(NullRenderer.Instance);
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
						if ((item2.FrameType == RenderTreeFrameType.Attribute) && (item2.AttributeName == RenderTreeBuilder.ChildContent) && (item2.AttributeValue is RenderFragment splitPartChildContent))
							parms.Add(new Tuple<string, RenderFragment>(propertyName, splitPartChildContent));
					}
					a = a + 2;
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

		class NullRenderer : Renderer
		{
			public NullRenderer(IServiceProvider serviceProvider) : base(serviceProvider)
			{ }

			protected override void UpdateDisplay(in RenderBatch renderBatch)
			{ }

			//internal static NullRenderer Instance { get; } = new NullRenderer(new BrowserServiceProvider());
			internal static NullRenderer Instance { get; } = new NullRenderer(new NullServiceProvider());

			class NullServiceProvider : IServiceProvider
			{
				public object GetService(Type serviceType)
				{
					throw new InvalidOperationException("This is something not finished yet, sorry.");
				}
			}
		}

		static class ParameterCollectionHelper
		{
			static FieldInfo _framesField = typeof(ParameterCollection).GetField("_frames", BindingFlags.NonPublic | BindingFlags.Instance);

			internal static RenderTreeFrame[] GetFrames(ParameterCollection parameters)
				=> (RenderTreeFrame[])_framesField.GetValue(parameters);

			internal static void SetFrames(ref ParameterCollection parameters, RenderTreeFrame[] frames)
			{
				object o = parameters;
				_framesField.SetValue(o, frames);
				parameters = (ParameterCollection)o;
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

	public class ContentProperty : BlazorComponent
	{
		[Parameter]
		string Name { get; set; } //this is not needed in runtime but looks better in razor view

		/*
		All the component is handled ContentsBlazorComponent so following is not used

		[Parameter]
		RenderFragment ChildContent { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			base.BuildRenderTree(builder);
			builder.AddContent(0, ChildContent);
		}*/
	}
}
