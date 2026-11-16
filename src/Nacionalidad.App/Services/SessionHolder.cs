using ExamEngine.Core.Models;
using ExamEngine.Core.Sessions;

namespace Nacionalidad.App.Services;

/// <summary>
/// Pasa la sesión en curso y su resultado entre páginas sin serializarlos en la URL de navegación.
/// </summary>
public sealed class SessionHolder
{
    public QuestionBank? Bank { get; private set; }
    public ExamSession? Session { get; private set; }
    public ExamResult? Result { get; private set; }

    public void Start(QuestionBank bank, ExamSession session)
    {
        Bank = bank;
        Session = session;
        Result = null;
    }

    public void Complete(ExamResult result) => Result = result;

    public void Clear()
    {
        Session = null;
        Result = null;
    }
}
