using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Team
{
    public interface ISectionView
    {
        ISectionViewModel ViewModel { get; }
    }
}
