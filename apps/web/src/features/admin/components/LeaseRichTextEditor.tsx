import { useEffect, useRef } from "react";
import {
  Bold,
  Italic,
  Underline,
  List,
  ListOrdered,
  Heading2,
  Heading3,
  AlignLeft,
  AlignCenter,
  AlignRight,
  Link2,
  Undo2,
  Redo2,
  RemoveFormatting,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type Props = {
  value: string;
  onChange: (html: string) => void;
  disabled?: boolean;
  className?: string;
};

function exec(command: string, value?: string) {
  document.execCommand(command, false, value);
}

export function LeaseRichTextEditor({ value, onChange, disabled, className }: Props) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    if (el.innerHTML !== value) {
      el.innerHTML = value || "<p></p>";
    }
  }, [value]);

  const run = (command: string, arg?: string) => {
    if (disabled) return;
    ref.current?.focus();
    exec(command, arg);
    onChange(ref.current?.innerHTML ?? "");
  };

  const insertLink = () => {
    const url = window.prompt("Link URL");
    if (!url) return;
    run("createLink", url);
  };

  return (
    <div className={cn("rounded-lg border bg-background", className)}>
      <div className="flex flex-wrap gap-1 border-b p-2">
        <ToolbarBtn title="Bold" onClick={() => run("bold")} disabled={disabled}>
          <Bold className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Italic" onClick={() => run("italic")} disabled={disabled}>
          <Italic className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Underline" onClick={() => run("underline")} disabled={disabled}>
          <Underline className="h-4 w-4" />
        </ToolbarBtn>
        <span className="mx-1 w-px self-stretch bg-border" />
        <ToolbarBtn title="Heading" onClick={() => run("formatBlock", "h2")} disabled={disabled}>
          <Heading2 className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Subheading" onClick={() => run("formatBlock", "h3")} disabled={disabled}>
          <Heading3 className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Paragraph" onClick={() => run("formatBlock", "p")} disabled={disabled}>
          P
        </ToolbarBtn>
        <span className="mx-1 w-px self-stretch bg-border" />
        <ToolbarBtn title="Bulleted list" onClick={() => run("insertUnorderedList")} disabled={disabled}>
          <List className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Numbered list" onClick={() => run("insertOrderedList")} disabled={disabled}>
          <ListOrdered className="h-4 w-4" />
        </ToolbarBtn>
        <span className="mx-1 w-px self-stretch bg-border" />
        <ToolbarBtn title="Align left" onClick={() => run("justifyLeft")} disabled={disabled}>
          <AlignLeft className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Align center" onClick={() => run("justifyCenter")} disabled={disabled}>
          <AlignCenter className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Align right" onClick={() => run("justifyRight")} disabled={disabled}>
          <AlignRight className="h-4 w-4" />
        </ToolbarBtn>
        <span className="mx-1 w-px self-stretch bg-border" />
        <ToolbarBtn title="Insert link" onClick={insertLink} disabled={disabled}>
          <Link2 className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Clear formatting" onClick={() => run("removeFormat")} disabled={disabled}>
          <RemoveFormatting className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Undo" onClick={() => run("undo")} disabled={disabled}>
          <Undo2 className="h-4 w-4" />
        </ToolbarBtn>
        <ToolbarBtn title="Redo" onClick={() => run("redo")} disabled={disabled}>
          <Redo2 className="h-4 w-4" />
        </ToolbarBtn>
      </div>
      <div
        ref={ref}
        contentEditable={!disabled}
        suppressContentEditableWarning
        className={cn(
          "prose prose-sm max-w-none min-h-[320px] px-4 py-3 focus:outline-none",
          disabled && "opacity-70",
        )}
        onInput={() => onChange(ref.current?.innerHTML ?? "")}
        onBlur={() => onChange(ref.current?.innerHTML ?? "")}
      />
    </div>
  );
}

function ToolbarBtn({
  children,
  onClick,
  disabled,
  title,
}: {
  children: React.ReactNode;
  onClick: () => void;
  disabled?: boolean;
  title: string;
}) {
  return (
    <Button
      type="button"
      variant="ghost"
      size="sm"
      className="h-8 w-8 p-0"
      title={title}
      disabled={disabled}
      onClick={onClick}
    >
      {children}
    </Button>
  );
}

/** Inserts plain text (e.g. a {{placeholder}}) at the current selection inside the editor. */
export function insertTextAtCursor(text: string) {
  const selection = window.getSelection();
  if (!selection || selection.rangeCount === 0) {
    document.execCommand("insertText", false, text);
    return;
  }
  const range = selection.getRangeAt(0);
  range.deleteContents();
  range.insertNode(document.createTextNode(text));
  range.collapse(false);
  selection.removeAllRanges();
  selection.addRange(range);
}
