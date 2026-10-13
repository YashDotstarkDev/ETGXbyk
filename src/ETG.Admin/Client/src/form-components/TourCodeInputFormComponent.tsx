import React from "react";
import { FormComponentProps } from "@kentico/xperience-admin-base";
import {
    FormEditMode,
    Input,
} from "@kentico/xperience-admin-components";

export const TourCodeInputFormComponent = (
    props: FormComponentProps<string>
) => {
    const handleChange = (
        event: React.ChangeEvent<HTMLInputElement>
    ) => {
        props.onChange?.(event.target.value);
    };

    return (
        <Input
            type= "text"
    name = { props.name }
    label = { props.label }
    value = { props.value ?? "" }
    onChange = { handleChange }
    invalid = { props.invalid }
    validationMessage = { props.validationMessage }
    disabled = { props.editMode === FormEditMode.Disabled }
    readOnly = { props.editMode === FormEditMode.ReadOnly }
    markAsRequired = { props.required }
    explanationText = { props.explanationText }
        />
    );
};