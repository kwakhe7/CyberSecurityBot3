using System.Windows.Controls;

namespace ChatbotPart2
{
    public partial class QuizView : UserControl
    {
        public QuizView()
        {
            InitializeComponent();
            this.DataContext = new QuizViewModel();
        }
    }
}