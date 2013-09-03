﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WF.Player.Core;

namespace Geowigo.Controls
{
	public class InputTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TextTemplate { get; set; }

		public DataTemplate MultipleChoiceTemplate { get; set; }
		
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			InputTypes iType = InputTypes.Unknown;

			if (item is Input)
			{
				iType = ((Input)item).InputType;
			}
			else if (item is Mockups.InputMockup)
			{
				iType = ((Mockups.InputMockup)item).InputType;
			}

			// Returns the proper template according to the type of the input.
			switch (iType)
			{
				case InputTypes.MultipleChoice:
					return MultipleChoiceTemplate;

				case InputTypes.Text:
					return TextTemplate;
	
				default:
					return base.SelectTemplate(item, container);
			}
		}
	}
}
