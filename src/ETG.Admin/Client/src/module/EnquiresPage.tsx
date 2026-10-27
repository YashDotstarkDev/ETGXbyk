import {
    useEffect,
    useMemo,
    useRef,
    useState,
    type CSSProperties,
    type FormEvent,
} from "react";
import { RoutingContentPlaceholder, usePageCommand } from "@kentico/xperience-admin-base";
import {
    Button,
    ButtonColor,
    ButtonSize,
    ButtonType,
    Icon,
    Pagination,
} from "@kentico/xperience-admin-components";
// Models
interface EnquiryItem {
    itemId: number;
    consultantName: string;
    formInserted: string;
    formUpdated: string;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    preferredContactMethod: string;
    preferredTourType: string;
    preferredDestination: string;
    preferredExperience: string;
    tourCode: string;
    tourName: string;
    tourLink: string;
    message: string;
    tourDate: string;
    tourClass: string;
    tourDepartureCity: string;
    tourDuration: string;
    tourQuotedPrice: number;
    priceCurrency: string;
    tourDiscountText: string;
    travellers: string;
    children: string;
    infants: string;
    subscribeToNewsletter: boolean;
    utmCampaign: string;
    utmContent: string;
    utmMedium: string;
    utmSource: string;
    utmTerm: string;
    comments: string;
    status: string;
    bookingReference: string;
    departureDate: string;
    returnDate: string;
    assignedUserGuid: string;
    storeView: string;
    isSpam: boolean;
}
interface EnquiryListArguments {
    page: number;
    pageSize: number;
    searchText: string;
    sortColumn: string;
    sortDescending: boolean;
}
interface EnquiryListResult {
    success: boolean;
    items: EnquiryItem[];
    totalCount: number;
    page: number;
    pageSize: number;
    errors: string[];
}
interface SaveEnquiryArguments {
    item: EnquiryItem;
}
interface SaveEnquiryResult {
    success: boolean;
    item?: EnquiryItem;
    errors: string[];
}
interface DeleteEnquiryArguments {
    itemId: number;
}
interface DeleteEnquiryResult {
    success: boolean;
    errors: string[];
}
type StringField = {
    [K in keyof EnquiryItem]: EnquiryItem[K] extends string ? K : never;
}[keyof EnquiryItem];
interface FieldDefinition {
    name: StringField;
    label: string;
    required?: boolean;
    type?: "text" | "email" | "tel" | "date";
    multiline?: boolean;
}
interface FieldSection {
    title: string;
    fields: FieldDefinition[];
}
interface ColumnDefinition {
    field: keyof EnquiryItem;
    label: string;
    sortable: boolean;
}
// Form fields
const fieldSections: FieldSection[] = [
    {
        title: "Contact information",
        fields: [
            { name: "firstName", label: "First Name", required: true },
            { name: "lastName", label: "Last Name", required: true },
            { name: "email", label: "Email", type: "email", required: true },
            { name: "phone", label: "Phone", type: "tel" },
            { name: "preferredContactMethod", label: "Preferred Contact Method" },
            { name: "preferredTourType", label: "Preferred Tour Type" },
            { name: "preferredDestination", label: "Preferred Destination" },
            { name: "preferredExperience", label: "Preferred Experience" },
        ],
    },
    {
        title: "Tour information",
        fields: [
            { name: "tourCode", label: "Tour Code" },
            { name: "tourName", label: "Tour Name" },
            { name: "tourLink", label: "Tour Link" },
            { name: "tourDate", label: "Tour Date" },
            { name: "tourClass", label: "Tour Class" },
            { name: "tourDepartureCity", label: "Tour Departure City" },
            { name: "tourDuration", label: "Tour Duration" },
            { name: "priceCurrency", label: "Price Currency" },
            { name: "tourDiscountText", label: "Discount" },
            { name: "travellers", label: "Travellers" },
            { name: "children", label: "Children" },
            { name: "infants", label: "Infants" },
            { name: "message", label: "Message", multiline: true },
        ],
    },
    {
        title: "Enquiry management",
        fields: [
            { name: "status", label: "Status" },
            { name: "bookingReference", label: "Booking Reference" },
            { name: "departureDate", label: "Departure Date", type: "date" },
            { name: "returnDate", label: "Return Date", type: "date" },
            { name: "assignedUserGuid", label: "Assigned User GUID" },
            { name: "storeView", label: "Store View" },
            { name: "comments", label: "Comments", multiline: true },
        ],
    },
    {
        title: "Tracking",
        fields: [
            { name: "utmCampaign", label: "UTM Campaign" },
            { name: "utmContent", label: "UTM Content" },
            { name: "utmMedium", label: "UTM Medium" },
            { name: "utmSource", label: "UTM Source" },
            { name: "utmTerm", label: "UTM Term" },
        ],
    },
];
const columns: ColumnDefinition[] = [
    { field: "itemId", label: "ID", sortable: true },
    { field: "formInserted", label: "Date created", sortable: true },
    { field: "preferredDestination", label: "Destination", sortable: true },
    { field: "status", label: "Status", sortable: true },
    { field: "consultantName", label: "Consultant", sortable: false },
    { field: "firstName", label: "First name", sortable: true },
    { field: "lastName", label: "Last name", sortable: true },
    { field: "email", label: "Email", sortable: true },
    { field: "phone", label: "Phone", sortable: true },
    {
        field: "preferredContactMethod",
        label: "Preferred contact method",
        sortable: true,
    },
    { field: "preferredExperience", label: "Experience", sortable: true },
    { field: "tourName", label: "Tour name", sortable: true },
    { field: "tourCode", label: "Tour code", sortable: true },
    { field: "utmCampaign", label: "UTM campaign", sortable: true },
    { field: "utmContent", label: "UTM content", sortable: true },
    { field: "utmMedium", label: "UTM medium", sortable: true },
    { field: "utmSource", label: "UTM source", sortable: true },
    { field: "utmTerm", label: "UTM term", sortable: true },
    { field: "formUpdated", label: "Last updated date", sortable: true },
];
// Default values
// Styles
const pageStyle: CSSProperties = {
    padding: 0,
    color: "#222",
    fontFamily: "inherit",
};
// Template
export const EnquiresTemplate = () => (
    <RoutingContentPlaceholder>
        <EnquiryScreen editMode={false} />
    </RoutingContentPlaceholder>
);
export const EnquiryEditTemplate = () => <EnquiryScreen editMode />;

const EnquiryScreen = ({ editMode }: { editMode: boolean }) => {
    const [items, setItems] = useState<EnquiryItem[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [page, setPage] = useState(1);
    const pageSize = 25;
    const [searchInput, setSearchInput] = useState("");
    const [searchText, setSearchText] = useState("");
    const [sortColumn, setSortColumn] = useState("FormInserted");
    const [sortDescending, setSortDescending] = useState(true);
    const [editorItem, setEditorItem] = useState<EnquiryItem | null>(null);
    const [deleteArguments, setDeleteArguments] = useState<DeleteEnquiryArguments>({
        itemId: 0,
    });
    const [loading, setLoading] = useState(editMode);
    const [saving, setSaving] = useState(false);
    const [deleting, setDeleting] = useState(false);
    const [message, setMessage] = useState("");
    const [messageSuccess, setMessageSuccess] = useState(false);
    const [refreshVersion, setRefreshVersion] = useState(0);
    const [exporting, setExporting] = useState(false);
    const busy = loading || saving || deleting || exporting;
    const listArguments = useMemo<EnquiryListArguments>(
        () => ({
            page,
            pageSize,
            searchText,
            sortColumn,
            sortDescending,
        }),
        [page, pageSize, searchText, sortColumn, sortDescending],
    );
    const latestListRequest = useRef<{
        args: EnquiryListArguments;
        version: number;
    } | null>(null);
    const listResponse = useRef<EnquiryListResult | undefined>(undefined);
    const listRunning = useRef(false);
    const mounted = useRef(true);
    const tableViewport = useRef<HTMLDivElement>(null);
    const horizontalScrollbar = useRef<HTMLDivElement>(null);
    const [tableScrollWidth, setTableScrollWidth] = useState(3600);
    useEffect(() => {
        if (editMode) return;
        const viewport = tableViewport.current;
        const scrollbar = horizontalScrollbar.current;
        const table = viewport?.querySelector("table");
        if (!viewport || !scrollbar || !table) return;
        const measure = () => setTableScrollWidth(viewport.scrollWidth);
        const syncFromTable = () => {
            if (scrollbar.scrollLeft !== viewport.scrollLeft)
                scrollbar.scrollLeft = viewport.scrollLeft;
        };
        const syncFromScrollbar = () => {
            if (viewport.scrollLeft !== scrollbar.scrollLeft)
                viewport.scrollLeft = scrollbar.scrollLeft;
        };
        const observer = new ResizeObserver(measure);
        observer.observe(viewport);
        observer.observe(table);
        viewport.addEventListener("scroll", syncFromTable, { passive: true });
        scrollbar.addEventListener("scroll", syncFromScrollbar, { passive: true });
        measure();
        return () => {
            observer.disconnect();
            viewport.removeEventListener("scroll", syncFromTable);
            scrollbar.removeEventListener("scroll", syncFromScrollbar);
        };
    }, [editMode]);
    useEffect(() => {
        mounted.current = true;
        return () => {
            mounted.current = false;
        };
    }, []);
    // Page commands
    const listCommand = usePageCommand<EnquiryListResult, EnquiryListArguments>(
        "GetEnquiries",
        {
            after: (response) => {
                listResponse.current = response;
            },
        },
    );
    const handleSaveResponse = (
        response: SaveEnquiryResult | undefined,
        creating: boolean,
    ) => {
        setSaving(false);
        if (!response?.success) {
            setMessageSuccess(false);
            setMessage(response?.errors?.join(" ") || "Unable to save the enquiry.");
            return;
        }
        if (response.item) setEditorItem(response.item);
        setMessageSuccess(true);
        setMessage(
            creating ? "Enquiry created successfully." : "Enquiry updated successfully.",
        );
        setRefreshVersion((current) => current + 1);
    };
    const updateCommand = usePageCommand<SaveEnquiryResult, SaveEnquiryArguments>(
        "UpdateEnquiry",
        {
            after: (response) => handleSaveResponse(response, false),
        },
    );
    const deleteCommand = usePageCommand<DeleteEnquiryResult, DeleteEnquiryArguments>(
        "DeleteEnquiry",
        {
            after: (response) => {
                if (!response?.success) {
                    setMessageSuccess(false);
                    setMessage(
                        response?.errors?.join(" ") || "Unable to delete the enquiry.",
                    );
                    return;
                }
                setMessageSuccess(true);
                setMessage("Enquiry deleted successfully.");
                setRefreshVersion((current) => current + 1);
            },
        },
    );
    const exportCommand = usePageCommand<
        {
            success: boolean;
            fileName: string;
            base64: string;
            errors: string[];
        },
        Record<string, never>
    >("ExportEnquiries", {
        data: {},
        after: (response) => {
            if (!response?.success) {
                setMessageSuccess(false);
                setMessage(response?.errors?.join(" ") || "Unable to export enquiries.");
                return;
            }
            const binary = atob(response.base64);
            const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
            const url = URL.createObjectURL(
                new Blob([bytes], {
                    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                }),
            );
            const link = document.createElement("a");
            link.href = url;
            link.download = response.fileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.setTimeout(() => URL.revokeObjectURL(url), 10000);
        },
    });
    const exportEnquiries = async () => {
        if (exporting) return;
        setExporting(true);
        setMessage("");
        try {
            await exportCommand.execute();
        } catch {
            setMessageSuccess(false);
            setMessage("Unable to export enquiries.");
        } finally {
            setExporting(false);
        }
    };
    useEffect(() => {
        const timer = window.setTimeout(() => {
            const text = searchInput.trim();
            if (text !== searchText) {
                setMessage("");
                setPage(1);
                setSearchText(text);
            }
        }, 400);
        return () => window.clearTimeout(timer);
    }, [searchInput, searchText]);

    const loadEditCommand = usePageCommand<SaveEnquiryResult>("LoadEnquiry", {
        after: (response) => {
            if (response?.success && response.item) {
                setEditorItem(response.item);
            } else {
                setMessageSuccess(false);
                setMessage(response?.errors?.join(" ") || "Unable to open the enquiry.");
            }
        },
    });
    const openEditCommand = usePageCommand<void, number>("OpenEnquiry");
    const backCommand = usePageCommand<void>("ReturnToEnquiries");
    const cancelEdit = async () => {
        if (saving) return;
        try {
            await backCommand.execute();
        } catch {
            setMessageSuccess(false);
            setMessage("Unable to return to enquiries.");
        }
    };
    useEffect(() => {
        if (!editMode) return;
        const load = async () => {
            setLoading(true);
            try {
                await loadEditCommand.execute();
            } catch {
                setMessageSuccess(false);
                setMessage("Unable to open the enquiry. Check the Event log.");
            } finally {
                setLoading(false);
            }
        };
        void load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [editMode]);

    // Serialize list requests and ignore a response when a newer page/search is pending.
    useEffect(() => {
        if (editMode) return;
        latestListRequest.current = { args: listArguments, version: refreshVersion };
        if (listRunning.current) return;
        const load = async () => {
            listRunning.current = true;
            setLoading(true);
            try {
                while (mounted.current && latestListRequest.current) {
                    const request = latestListRequest.current;
                    listResponse.current = undefined;
                    try {
                        await listCommand.execute(request.args);
                        if (!mounted.current) return;
                        if (latestListRequest.current !== request) continue;
                        const response = listResponse.current as
                            EnquiryListResult | undefined;
                        if (!response?.success) {
                            setItems([]);
                            setTotalCount(0);
                            setMessageSuccess(false);
                            setMessage(
                                response?.errors?.join(" ") ||
                                    "Unable to load enquiries.",
                            );
                        } else {
                            setItems(response.items ?? []);
                            setTotalCount(response.totalCount ?? 0);
                            if (response.page > 0) setPage(response.page);
                            if (tableViewport.current)
                                tableViewport.current.scrollTop = 0;
                        }
                    } catch {
                        if (!mounted.current) return;
                        if (latestListRequest.current !== request) continue;
                        setMessageSuccess(false);
                        setMessage("Unable to load enquiries. Check the Event log.");
                    }
                    latestListRequest.current = null;
                }
            } finally {
                listRunning.current = false;
                if (mounted.current) setLoading(false);
            }
        };
        void load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [editMode, listArguments, refreshVersion]);
    useEffect(() => {
        if (deleteArguments.itemId <= 0) {
            return;
        }
        const remove = async () => {
            try {
                await deleteCommand.execute(deleteArguments);
            } catch {
                setMessageSuccess(false);
                setMessage("Unable to delete the enquiry. Check the Event log.");
            } finally {
                setDeleting(false);
                setDeleteArguments({ itemId: 0 });
            }
        };
        void remove();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [deleteArguments.itemId]);
    // Search and sorting
    const submitSearch = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setMessage("");
        setPage(1);
        setSearchText(searchInput.trim());
        setRefreshVersion((current) => current + 1);
    };
    const changeSort = (field: keyof EnquiryItem) => {
        setMessage("");
        setPage(1);
        if (sortColumn.toLowerCase() === field.toLowerCase()) {
            setSortDescending((current) => !current);
        } else {
            setSortColumn(field);
            setSortDescending(false);
        }
    };
    // Create, edit and delete
    const updateEditor = <K extends keyof EnquiryItem>(
        field: K,
        value: EnquiryItem[K],
    ) => {
        setEditorItem((current) => (current ? { ...current, [field]: value } : current));
    };
    const saveEditor = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!editorItem || busy) {
            return;
        }
        if (!editorItem.firstName.trim()) {
            setMessageSuccess(false);
            setMessage("First Name is required.");
            return;
        }
        if (!editorItem.lastName.trim()) {
            setMessageSuccess(false);
            setMessage("Last Name is required.");
            return;
        }
        if (!editorItem.email.trim()) {
            setMessageSuccess(false);
            setMessage("Email is required.");
            return;
        }
        if (
            !Number.isFinite(editorItem.tourQuotedPrice) ||
            editorItem.tourQuotedPrice < 0
        ) {
            setMessageSuccess(false);
            setMessage("Enter a valid non-negative quoted price.");
            return;
        }
        setSaving(true);
        setMessage("");
        try {
            await updateCommand.execute({ item: editorItem });
        } catch {
            setMessageSuccess(false);
            setMessage("Unable to save the enquiry. Check the Event log.");
        } finally {
            setSaving(false);
        }
    };
    const deleteEnquiry = (item: EnquiryItem) => {
        if (busy) {
            return;
        }
        const name =
            `${item.firstName} ${item.lastName}`.trim() || `submission ${item.itemId}`;
        if (!window.confirm(`Delete ${name}?`)) {
            return;
        }
        setMessage("");
        setDeleting(true);
        setDeleteArguments({ itemId: item.itemId });
    };
    // Shared message
    const messageElement = message ? (
        <div
            role={messageSuccess ? "status" : "alert"}
            style={{
                padding: "12px 16px",
                marginBottom: 18,
                borderRadius: 4,
                background: messageSuccess ? "#edf7ee" : "#fdeeee",
                color: messageSuccess ? "#1b5e20" : "#8e1b1b",
            }}
        >
            {message}
        </div>
    ) : null;
    if (editMode) {
        return (
            <div className="etg-enquiries enquiry-editor" style={pageStyle}>
                <style>{listingCss}</style>
                <form id="enquiry-edit-form" onSubmit={saveEditor}>
                    <div className="enquiry-edit-toolbar">
                        <Button
                            type={ButtonType.Submit}
                            color={ButtonColor.Primary}
                            size={ButtonSize.M}
                            label={saving ? "SAVING…" : "SAVE"}
                            disabled={busy || !editorItem}
                            inProgress={saving}
                        />
                        <Button
                            type={ButtonType.Button}
                            color={ButtonColor.Secondary}
                            size={ButtonSize.M}
                            label="CANCEL"
                            disabled={saving}
                            onClick={() => void cancelEdit()}
                        />
                    </div>
                    {messageElement}
                    {!editorItem && (
                        <p role="status">
                            {loading
                                ? "Loading enquiry…"
                                : "Enquiry could not be loaded."}
                        </p>
                    )}
                    {editorItem && (
                        <div className="enquiry-edit-card">
                            <h1>Edit Enquiry {editorItem.itemId}</h1>
                            <p className="enquiry-edit-meta">
                                Created: {editorItem.formInserted} · Last updated:{" "}
                                {editorItem.formUpdated}
                            </p>
                            {fieldSections.map((section) => (
                                <section key={section.title}>
                                    <h2 className="enquiry-section-heading">
                                        {section.title}
                                    </h2>
                                    {section.fields.map((field) => (
                                        <div
                                            className="enquiry-edit-field"
                                            key={field.name}
                                        >
                                            <label htmlFor={`enquiry-${field.name}`}>
                                                {field.required && (
                                                    <span className="enquiry-required">
                                                        *
                                                    </span>
                                                )}
                                                {field.label}
                                            </label>
                                            {field.multiline ? (
                                                <textarea
                                                    id={`enquiry-${field.name}`}
                                                    rows={4}
                                                    value={editorItem[field.name] ?? ""}
                                                    disabled={saving}
                                                    onChange={(event) =>
                                                        updateEditor(
                                                            field.name,
                                                            event.target.value,
                                                        )
                                                    }
                                                />
                                            ) : (
                                                <input
                                                    id={`enquiry-${field.name}`}
                                                    type={field.type ?? "text"}
                                                    required={field.required}
                                                    value={editorItem[field.name] ?? ""}
                                                    disabled={saving}
                                                    onChange={(event) =>
                                                        updateEditor(
                                                            field.name,
                                                            event.target.value,
                                                        )
                                                    }
                                                />
                                            )}
                                        </div>
                                    ))}
                                    {section.title === "Tour information" && (
                                        <div className="enquiry-edit-field">
                                            <label htmlFor="enquiry-price">
                                                Quoted Price
                                            </label>
                                            <input
                                                id="enquiry-price"
                                                type="number"
                                                min={0}
                                                step={1}
                                                value={editorItem.tourQuotedPrice}
                                                disabled={saving}
                                                onChange={(event) =>
                                                    updateEditor(
                                                        "tourQuotedPrice",
                                                        Number(event.target.value),
                                                    )
                                                }
                                            />
                                        </div>
                                    )}
                                    {section.title === "Enquiry management" && (
                                        <>
                                            <label className="enquiry-checkbox">
                                                <input
                                                    type="checkbox"
                                                    checked={
                                                        editorItem.subscribeToNewsletter
                                                    }
                                                    disabled={saving}
                                                    onChange={(event) =>
                                                        updateEditor(
                                                            "subscribeToNewsletter",
                                                            event.target.checked,
                                                        )
                                                    }
                                                />
                                                Subscribe to Newsletter
                                            </label>
                                            <label className="enquiry-checkbox">
                                                <input
                                                    type="checkbox"
                                                    checked={editorItem.isSpam}
                                                    disabled={saving}
                                                    onChange={(event) =>
                                                        updateEditor(
                                                            "isSpam",
                                                            event.target.checked,
                                                        )
                                                    }
                                                />
                                                Spam
                                            </label>
                                        </>
                                    )}
                                </section>
                            ))}
                        </div>
                    )}
                </form>
            </div>
        );
    }
    // Listing: first/last pages, current page, next page and skipped-range ellipses.
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

    const openEditor = async (item: EnquiryItem) => {
        if (busy || item.itemId <= 0) return;
        setMessage("");
        setLoading(true);
        try {
            await openEditCommand.execute(item.itemId);
        } catch {
            setMessageSuccess(false);
            setMessage("Unable to open the edit page. Check the Event log.");
        } finally {
            setLoading(false);
        }
    };
    return (
        <div className="etg-enquiries" style={pageStyle}>
            <style>{listingCss}</style>
            <h1>Enquiries</h1>
            {messageElement}
            <div className="enquiry-export-row">
                <Button
                    type={ButtonType.Button}
                    color={ButtonColor.Primary}
                    size={ButtonSize.M}
                    label="EXPORT EXCEL"
                    disabled={saving || deleting || exporting}
                    inProgress={exporting}
                    onClick={() => void exportEnquiries()}
                />
            </div>
            <div className="enquiry-toolbar">
                <form onSubmit={submitSearch} className="enquiry-search">
                    <input
                        type="search"
                        aria-label="Search enquiries"
                        maxLength={200}
                        placeholder={searchHint}
                        value={searchInput}
                        disabled={saving || deleting}
                        onChange={(event) => setSearchInput(event.target.value)}
                    />
                    <button
                        type="submit"
                        className="enquiry-search-icon"
                        disabled={saving || deleting}
                        aria-label="Search enquiries"
                    >
                        <svg
                            width="18"
                            height="18"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            strokeWidth="1.5"
                            aria-hidden="true"
                        >
                            <circle cx="10.5" cy="10.5" r="6.5" />
                            <path d="m16 16 5 5" />
                        </svg>
                    </button>
                </form>
                <Button
                    type={ButtonType.Button}
                    color={ButtonColor.Secondary}
                    size={ButtonSize.M}
                    label="CANCEL"
                    disabled={saving || deleting}
                    onClick={() => {
                        setMessage("");
                        setSearchInput("");
                        setSearchText("");
                        setPage(1);
                        setRefreshVersion((value) => value + 1);
                    }}
                />
                <Button
                    type={ButtonType.Button}
                    color={ButtonColor.Secondary}
                    size={ButtonSize.M}
                    label="REFRESH"
                    disabled={saving || deleting}
                    onClick={() => {
                        setMessage("");
                        setRefreshVersion((value) => value + 1);
                    }}
                />
            </div>
            <span className="enquiry-loading-status" role="status" aria-live="polite">
                {loading
                    ? "Loading enquiries…"
                    : exporting
                      ? "Preparing Excel export…"
                      : ""}
            </span>
            {!loading && !message && !items.length && <p>No enquiries were found.</p>}
            <div className="enquiry-scroll" aria-busy={loading} ref={tableViewport}>
                <table className="enquiry-table">
                    <thead>
                        <tr>
                            {columns.map((column) => {
                                const selected =
                                    sortColumn.toLowerCase() ===
                                    column.field.toLowerCase();
                                return (
                                    <th
                                        key={column.field}
                                        scope="col"
                                        aria-sort={
                                            selected
                                                ? sortDescending
                                                    ? "descending"
                                                    : "ascending"
                                                : "none"
                                        }
                                    >
                                        {column.sortable ? (
                                            <button
                                                type="button"
                                                disabled={saving || deleting}
                                                onClick={() => changeSort(column.field)}
                                            >
                                                {column.label}
                                                {selected
                                                    ? sortDescending
                                                        ? " ↓"
                                                        : " ↑"
                                                    : ""}
                                            </button>
                                        ) : (
                                            column.label
                                        )}
                                    </th>
                                );
                            })}
                            <th scope="col">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {items.map((item) => (
                            <tr key={item.itemId} onClick={() => openEditor(item)}>
                                {columns.map((column) => (
                                    <td key={column.field}>
                                        {column.field === "itemId" ? (
                                            <button
                                                type="button"
                                                className="enquiry-id"
                                                disabled={saving || deleting}
                                                aria-label={`Edit enquiry ${item.itemId}`}
                                                onClick={(event) => {
                                                    event.stopPropagation();
                                                    openEditor(item);
                                                }}
                                            >
                                                {item.itemId}
                                            </button>
                                        ) : (
                                            String(item[column.field] ?? "")
                                        )}
                                    </td>
                                ))}
                                <td>
                                    <div
                                        className="enquiry-actions"
                                        onClick={(event) => event.stopPropagation()}
                                    >
                                        <button
                                            type="button"
                                            disabled={saving || deleting}
                                            className="enquiry-row-action enquiry-delete"
                                            title="Delete enquiry"
                                            aria-label={`Delete enquiry ${item.itemId}`}
                                            onClick={() => deleteEnquiry(item)}
                                        >
                                            <Icon name="xp-bin" />
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            <div className="enquiry-footer">
                <Pagination
                    selectedPage={page}
                    totalPages={totalPages}
                    onPageChange={(nextPage) => {
                        if (saving || deleting) return;
                        setMessage("");
                        setPage(Math.min(totalPages, Math.max(1, nextPage)));
                    }}
                />
            </div>
            <div
                className="enquiry-horizontal-scroll"
                ref={horizontalScrollbar}
                role="region"
                aria-label="Scroll enquiry columns horizontally"
                tabIndex={0}
            >
                <div style={{ width: tableScrollWidth, height: 1 }} />
            </div>
        </div>
    );
};
const searchHint =
    "Search by destination, status, first name, last name, email, phone, experience, tour name or tour code";
const listingCss = `
.etg-enquiries { position:relative; }
.etg-enquiries .enquiry-loading-status { position:absolute; width:1px; height:1px; padding:0; margin:-1px; overflow:hidden; clip-path:inset(50%); white-space:nowrap; border:0; }
.etg-enquiries .enquiry-export-row > button { min-width:145px; }

.etg-enquiries .enquiry-edit-toolbar { display:flex; gap:12px; margin:0 0 28px; }
.etg-enquiries .enquiry-edit-card { background:white; border-radius:16px; padding:28px 30px; box-shadow:0 6px 18px #21102712; }
.etg-enquiries .enquiry-section-heading { font-size:16px; font-weight:600; margin:24px 0 18px; }
.etg-enquiries .enquiry-edit-meta { color:#6d6470; font-size:12px; }
.etg-enquiries .enquiry-edit-field { margin-bottom:24px; }
.etg-enquiries .enquiry-edit-field label { display:block; font-size:14px; margin-bottom:10px; }
.etg-enquiries .enquiry-edit-field input,.etg-enquiries .enquiry-edit-field textarea { width:100%; box-sizing:border-box; border:1px solid #8d858e; border-radius:22px; padding:9px 16px; min-height:38px; font:inherit; background:white; color:inherit; }
.etg-enquiries .enquiry-edit-field textarea { border-radius:14px; resize:vertical; }
.etg-enquiries .enquiry-required { color:#ed4c5c; }
.etg-enquiries .enquiry-checkbox { display:flex; gap:8px; align-items:center; margin-bottom:22px; font-size:14px; }
.etg-enquiries .enquiry-checkbox input { accent-color:#680099; }

.etg-enquiries { color:inherit; font-family:inherit; }
.etg-enquiries h1 { font-size:20px; line-height:28px; font-weight:700; margin:0 0 20px; }
.etg-enquiries .enquiry-export-row { margin-bottom:16px; }
.etg-enquiries .enquiry-toolbar { display:flex; gap:16px; align-items:center; margin-bottom:12px; flex-wrap:wrap; }
.etg-enquiries .enquiry-search { flex:1 1 420px; min-width:220px; position:relative; margin:0; }
.etg-enquiries .enquiry-search input { display:block; width:100%; height:36px; border:1px solid #8d858e; border-radius:20px; padding:0 40px 0 16px; background:transparent; color:inherit; font:inherit; font-size:12px; box-sizing:border-box; }
.etg-enquiries .enquiry-search input:focus { outline:2px solid #680099; outline-offset:2px; }
.etg-enquiries .enquiry-search input::-webkit-search-cancel-button { cursor:pointer; }
.etg-enquiries .enquiry-search-icon { position:absolute; right:10px; top:50%; transform:translateY(-50%); width:24px; height:24px; border:0; padding:3px; color:#6d6470; background:transparent; cursor:pointer; }
.etg-enquiries .enquiry-scroll { overflow-x:hidden; overflow-y:auto; max-height:calc(100dvh - 390px); min-height:180px; scrollbar-gutter:stable; }
.etg-enquiries .enquiry-horizontal-scroll { overflow-x:auto; overflow-y:hidden; width:100%; margin-top:8px; min-height:16px; }
.etg-enquiries .enquiry-table { width:100%; min-width:3600px; border-collapse:separate; border-spacing:0 4px; }
.etg-enquiries .enquiry-table th { position:sticky; top:0; z-index:2; background:#f3efef; padding:14px 24px; text-align:left; font-size:13px; font-weight:700; white-space:nowrap; min-width:160px; }
.etg-enquiries .enquiry-table th:first-child { min-width:75px; }
.etg-enquiries .enquiry-table th button { border:0; padding:0; background:transparent; color:inherit; font:inherit; font-weight:700; white-space:nowrap; cursor:pointer; }
.etg-enquiries .enquiry-table td { padding:12px 24px; height:46px; box-sizing:border-box; vertical-align:middle; background:#fff; border-top:1px solid #d8d5da; border-bottom:1px solid #d8d5da; font-size:13px; white-space:nowrap; }
.etg-enquiries .enquiry-table td:first-child { border-left:1px solid #d8d5da; border-radius:6px 0 0 6px; }
.etg-enquiries .enquiry-table th:last-child { right:0; z-index:3; min-width:70px; }
.etg-enquiries .enquiry-table td:last-child { position:sticky; right:0; z-index:1; box-shadow:-8px 0 12px -12px #33253c; border-right:1px solid #d8d5da; border-radius:0 6px 6px 0; }
.etg-enquiries .enquiry-table tbody tr { cursor:pointer; }
.etg-enquiries .enquiry-table tbody tr:hover td { background:#faf7fc; }
.etg-enquiries .enquiry-id { padding:0; border:0; background:transparent; color:inherit; font:inherit; cursor:pointer; }
.etg-enquiries .enquiry-actions { display:flex; gap:8px; align-items:center; }
.etg-enquiries .enquiry-row-action { border:0; background:transparent; color:#680099; padding:3px; width:24px; height:24px; cursor:pointer; }
.etg-enquiries .enquiry-delete { color:#ff354a; display:inline-flex; align-items:center; justify-content:center; border:0; background:transparent; }
.etg-enquiries .enquiry-delete svg { width:16px; height:16px; }
.etg-enquiries .enquiry-delete:hover:not(:disabled) { background:#fff0f2; border-radius:4px; }
.etg-enquiries .enquiry-delete:disabled { opacity:.45; cursor:default; }
.etg-enquiries .enquiry-footer { display:flex; justify-content:space-between; gap:16px; align-items:center; flex-wrap:wrap; margin-top:8px; }
.etg-enquiries .enquiry-count { font-size:12px; color:#6d6470; }
.etg-enquiries .enquiry-row-action:focus-visible,.etg-enquiries .enquiry-id:focus-visible { outline:2px solid #680099; outline-offset:2px; }

.etg-enquiries .enquiry-table th:last-child,
.etg-enquiries .enquiry-table td:last-child {
    position:sticky; right:0; width:80px; min-width:80px; max-width:80px;
    padding:10px 14px; box-sizing:border-box;
    box-shadow:-10px 0 14px -10px #0003;
}
.etg-enquiries .enquiry-table th:last-child { z-index:4; background:#f3efef; }
.etg-enquiries .enquiry-table td:last-child { z-index:2; background:#fff; border-radius:0; }
.etg-enquiries .enquiry-actions { justify-content:flex-start; }
.etg-enquiries .enquiry-delete {
    display:inline-flex; align-items:center; justify-content:center;
    width:28px; height:28px; padding:6px; border:0;
    background:transparent; color:#ff354a;
}
.etg-enquiries .enquiry-delete svg { width:16px; height:16px; }
`;
