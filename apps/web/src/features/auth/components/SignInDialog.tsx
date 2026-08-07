import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { SignInForm } from "@/features/auth/components/SignInForm";

type SignInDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title?: string;
  description?: string;
  /** Fires after a successful sign-in; keep the user on the current page. */
  onSuccess?: () => void | Promise<void>;
};

/**
 * In-place sign-in modal so guests can authenticate without navigating away
 * from the page that prompted them (e.g. saving a listing).
 */
export function SignInDialog({
  open,
  onOpenChange,
  title = "Sign in",
  description = "Sign in to continue.",
  onSuccess,
}: SignInDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-w-md"
        onClick={(e) => {
          // Listing cards wrap triggers in a <Link>; stop portal clicks from
          // bubbling into that navigation.
          e.stopPropagation();
        }}
      >
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        <SignInForm
          variant="dialog"
          onSuccess={async () => {
            onOpenChange(false);
            await onSuccess?.();
          }}
        />
      </DialogContent>
    </Dialog>
  );
}
