using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using F1;

namespace F1Converter
{
	public class FormTargetChip
	{
		public string chipName;
		public List<int> usableClocks = new List<int>();
		public int chipClock;
		public int chipTopCode;
		public int selectedIndex;
	}

	public class FormTarget
	{
		public string name;
		public byte[] commandArray;
		public List<FormTargetChip> formTargetChips = new List<FormTargetChip>();
	}
}
