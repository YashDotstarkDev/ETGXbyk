import { FormComponentProps } from "@kentico/xperience-admin-base";
import {
    Checkbox,
    FormEditMode,
    FormItemWrapper,
} from "@kentico/xperience-admin-components";

interface MultiChoiceOption {
    value: string;
    label: string;
}

interface MultiChoiceSelectorProps extends FormComponentProps<string> {
    options?: MultiChoiceOption[];
}

export const MultiChoiceSelectorFormComponent = (
    props: MultiChoiceSelectorProps
) => {
    const selectedValues = (props.value ?? "")
        .split(";")
        .map((value) => value.trim())
        .filter((value) => value.length > 0);

    const disabled =
        props.editMode === FormEditMode.Disabled ||
        props.editMode === FormEditMode.ReadOnly;

    const changeSelection = (optionValue: string, checked: boolean) => {
        const updatedValues = checked
            ? [
                ...selectedValues.filter(
                    (value) => value !== optionValue
                ),
                optionValue,
            ]
            : selectedValues.filter(
                (value) => value !== optionValue
            );

        props.onChange?.(updatedValues.join(";"));
    };

    return (
        <FormItemWrapper
            label= { props.label }
    explanationText = { props.explanationText }
    invalid = { props.invalid }
    validationMessage = { props.validationMessage }
    markAsRequired = { props.required }
    editMode = { props.editMode }
        >
        <div
                style={
        {
            display: "flex",
                flexDirection: "column",
                    alignItems: "flex-start",
                        gap: "12px",
                }
    }
            >
        {(props.options ?? []).map((option) => (
            <div key= { option.value } >
            <Checkbox
                            label={ option.label }
                            checked = { selectedValues.includes(option.value) }
                            disabled = { disabled }
                            onChange = {(_event, checked) =>
changeSelection(option.value, checked)
                            }
                        />
    </div>
                ))}
</div>
    </FormItemWrapper>
    );
};