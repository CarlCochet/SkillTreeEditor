using ComboBox = System.Windows.Controls.ComboBox;

namespace SkillTreeEditor;

public class ControlHandler
{
    public static int? GetNullableIntFromComboBox(ComboBox comboBox)
    {
        return comboBox.SelectedValue switch
        {
            int value => value,
            _ => null
        };
    }
}