import { useMemo, useRef, useState, type ChangeEvent, type CSSProperties } from "react";
import { usePageCommand } from "@kentico/xperience-admin-base";
import { Button, ButtonType } from "@kentico/xperience-admin-components";

interface ImportResult {
    success: boolean;
    rowCount: number;
    importedCount: number;
    skippedCount: number;
    errors: string[];
}

interface ImportArguments {
    csvText: string;
}

type LastAction = "validate" | "import";

const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB
const MAX_ERRORS_SHOWN = 100;

const emptyResult = (error: string): ImportResult => ({
    success: false,
    rowCount: 0,
    importedCount: 0,
    skippedCount: 0,
    errors: [error],
});

/* ---------- Styling (neutral admin look, no hard-coded Kentico class hashes) ---------- */

const palette = {
    success: { background: "#edf7ee", border: "#2e7d32", text: "#1b5e20" },
    error: { background: "#fdeeee", border: "#c62828", text: "#8e1b1b" },
    warning: { background: "#fff8e1", border: "#f9a825", text: "#5f4600" },
};

const pageStyle: CSSProperties = {
    padding: 24,
    maxWidth: 900,
    color: "#1f1f1f",
    fontFamily: "inherit",
};

const cardStyle: CSSProperties = {
    background: "#ffffff",
    borderRadius: 8,
    boxShadow: "0 1px 4px rgba(0, 0, 0, 0.12)",
    padding: 24,
};

const headingStyle: CSSProperties = {
    margin: "0 0 8px",
    fontSize: 24,
    fontWeight: 600,
    color: "#1f1f1f",
};

const mutedStyle: CSSProperties = {
    margin: 0,
    fontSize: 14,
    lineHeight: 1.5,
    color: "#5c5c5c",
};

const rowStyle: CSSProperties = {
    display: "flex",
    alignItems: "center",
    gap: 12,
    flexWrap: "wrap",
};

const sectionStyle: CSSProperties = { marginTop: 24 };

const boxStyle = (tone: keyof typeof palette): CSSProperties => ({
    marginTop: 24,
    padding: "16px 20px",
    borderRadius: 6,
    borderLeft: `4px solid ${palette[tone].border}`,
    background: palette[tone].background,
    color: palette[tone].text,
});

export const ImportBookingPricesTemplate = () => {
    const fileInputRef = useRef<HTMLInputElement>(null);

    const [csvText, setCsvText] = useState("");
    const [fileName, setFileName] = useState("");
    const [fileSize, setFileSize] = useState(0);
    const [busy, setBusy] = useState(false);
    const [confirmingImport, setConfirmingImport] = useState(false);
    const [lastAction, setLastAction] = useState<LastAction | null>(null);
    const [result, setResult] = useState<ImportResult | null>(null);

    const commandData = useMemo<ImportArguments>(() => ({ csvText }), [csvText]);

    const validateCommand = usePageCommand<ImportResult, ImportArguments>("ValidateCsv", {
        data: commandData,
        after: (response: ImportResult | undefined) => {
            setResult(response ?? null);
            setBusy(false);
        },
    });

    const importCommand = usePageCommand<ImportResult, ImportArguments>("ImportCsv", {
        data: commandData,
        after: (response: ImportResult | undefined) => {
            setResult(response ?? null);
            setBusy(false);
        },
    });

    const resetFile = () => {
        setCsvText("");
        setFileName("");
        setFileSize(0);
    };

    const onFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
        const input = event.target;
        const file = input.files?.[0];

        setResult(null);
        setLastAction(null);
        setConfirmingImport(false);
        resetFile();

        if (!file) {
            return;
        }

        try {
            if (!file.name.toLowerCase().endsWith(".csv")) {
                setResult(emptyResult("Choose a .csv file."));
                return;
            }

            if (file.size > MAX_FILE_SIZE_BYTES) {
                setResult(emptyResult("The file is too large. The maximum size is 5 MB."));
                return;
            }

            const text = await file.text();

            if (!text.trim()) {
                setResult(emptyResult("The selected CSV file is empty."));
                return;
            }

            setCsvText(text);
            setFileName(file.name);
            setFileSize(file.size);
        } catch {
            setResult(emptyResult("Could not read the selected CSV file."));
        } finally {
            // Allows choosing the same file again after editing it.
            input.value = "";
        }
    };

    const onValidate = async () => {
        setConfirmingImport(false);
        setLastAction("validate");
        setBusy(true);
        setResult(null);

        try {
            await validateCommand.execute();
        } catch {
            setBusy(false);
            setResult(emptyResult("Validation failed. Check the server log and try again."));
        }
    };

    const onImport = async () => {
        setConfirmingImport(false);
        setLastAction("import");
        setBusy(true);
        setResult(null);

        try {
            await importCommand.execute();
        } catch {
            setBusy(false);
            setResult(emptyResult("Import failed. Check the server log and try again."));
        }
    };

    const hasFile = csvText.length > 0;

    const renderResult = () => {
        if (!result) {
            return null;
        }

        if (result.success) {
            const isImport = lastAction === "import";

            return (
                <div role= "status" style = { boxStyle("success") } >
                    <strong style={ { fontSize: 16 } }>
                    { isImport? "Import completed": "CSV is valid" }
                        </strong>
                        < p style = {{ margin: "6px 0 0", fontSize: 14 }
        }>
            {
                isImport
                ? `${result.importedCount} rows were imported.`
                            : `${result.rowCount} rows checked. You can now click Import.`}
</p>
{
    result.skippedCount > 0 && (
        <p style={ { margin: "6px 0 0", fontSize: 14 } }>
        { result.skippedCount } blank or placeholder rows were skipped.
                        </p>
                    )
}
</div>
            );
        }

const shownErrors = result.errors.slice(0, MAX_ERRORS_SHOWN);
const hiddenCount = result.errors.length - shownErrors.length;

return (
    <div role= "alert" style = { boxStyle("error") } >
        <strong style={ { fontSize: 16 } }>
            There { result.errors.length === 1 ? "is 1 problem" : `are ${result.errors.length} problems` } with this CSV
                </strong>
                < p style = {{ margin: "6px 0 0", fontSize: 14 }}>
                    No prices were changed.Fix the following and try again:
</p>
    < ul style = {{ margin: "10px 0 0", paddingLeft: 20, fontSize: 14, lineHeight: 1.6 }}>
    {
        shownErrors.map((error, index) => (
            <li key= {`${index}-${error}`} > { error } </li>
                    ))}
</ul>
{
    hiddenCount > 0 && (
        <p style={ { margin: "8px 0 0", fontSize: 14 } }>
                        …and { hiddenCount } more.Fix these first, then validate again.
                    </p>
                )
}
</div>
        );
    };

return (
    <div style= { pageStyle } >
    <div style={ cardStyle }>
        <h1 style={ headingStyle }> Import Booking Prices </h1>
            < p style = { mutedStyle } >
                Upload the pricing CSV.This website imports columns A–D only: Package Code,
                    Start Date, End Date, and Twin Share Per Person Price.All other columns are
ignored.
                </p>

{/* Step 1 – choose file */ }
<div style={ sectionStyle }>
    <div style={ rowStyle }>
        <input
                            ref={ fileInputRef }
type = "file"
accept = ".csv,text/csv"
onChange = { onFileChange }
disabled = { busy }
style = {{ display: "none" }}
                        />
    < Button
label = "Choose file"
type = { ButtonType.SECONDARY }
disabled = { busy }
onClick = {() => fileInputRef.current?.click()}
                        />
    < span style = { mutedStyle } >
        {
            hasFile
            ? `${fileName} (${Math.max(1, Math.round(fileSize / 1024))} KB)`
                                : "No file chosen"}
</span>
    </div>
    </div>

{/* Step 2 – validate / import */ }
<div style={ { ...sectionStyle, ...rowStyle } }>
    <Button
                        label="Validate CSV File"
type = { ButtonType.SECONDARY }
disabled = { busy || !hasFile}
onClick = { onValidate }
    />
    <Button
                        label="Import"
type = { ButtonType.PRIMARY }
disabled = { busy || !hasFile}
onClick = {() => setConfirmingImport(true)}
                    />
    </div>

{ busy && <p style={ { ...mutedStyle, marginTop: 16 } }> Working…</p> }

{/* Confirm before replacing prices */ }
{
    confirmingImport && !busy && (
        <div role="alertdialog" style = { boxStyle("warning") } >
            <strong style={ { fontSize: 16 } }> Replace existing prices ? </strong>
                < p style = {{ margin: "6px 0 14px", fontSize: 14 }
}>
    All existing Booking Prices for every package code in this file will be
                            deleted and replaced with the rows from the CSV.Package codes that are
not in the CSV are not touched.
                        </p>
    < div style = { rowStyle } >
        <Button
                                label="Yes, import"
type = { ButtonType.PRIMARY }
onClick = { onImport }
    />
    <Button
                                label="Cancel"
type = { ButtonType.SECONDARY }
onClick = {() => setConfirmingImport(false)}
                            />
    </div>
    </div>
                )}

{ renderResult() }
</div>
    </div>
    );
};