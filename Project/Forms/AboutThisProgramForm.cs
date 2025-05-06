using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F1Converter
{
	public partial class AboutThisProgramForm : Form
	{
		public AboutThisProgramForm()
		{
			InitializeComponent();
			label2.Text = Resources.Strings.Version;
			this.Refresh();
		}
	}
}
