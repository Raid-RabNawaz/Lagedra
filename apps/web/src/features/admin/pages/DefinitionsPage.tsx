import { useEffect, useState, useCallback } from "react";
import {
  Plus,
  Pencil,
  Search,
  ShieldCheck,
  AlertTriangle,
  Sparkles,
} from "lucide-react";
import { listingApi } from "@/features/listings/services/listingApi";
import type {
  AmenityDefinitionDto,
  SafetyDeviceDefinitionDto,
  ConsiderationDefinitionDto,
  AmenityCategory,
  CreateAmenityDefinitionRequest,
  UpdateAmenityDefinitionRequest,
  CreateSafetyDeviceDefinitionRequest,
  UpdateSafetyDeviceDefinitionRequest,
  CreateConsiderationDefinitionRequest,
  UpdateConsiderationDefinitionRequest,
} from "@/api/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Loader } from "@/components/shared/Loader";
import { DynamicIcon } from "@/features/listings/components/DynamicIcon";

const amenityCategories: AmenityCategory[] = [
  "Kitchen",
  "Bathroom",
  "Bedroom",
  "LivingArea",
  "Outdoor",
  "Parking",
  "Entertainment",
  "WorkSpace",
  "Accessibility",
  "Laundry",
  "ClimateControl",
  "Internet",
];

export const DefinitionsPage = () => {
  const [tab, setTab] = useState("amenities");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Listing Definitions</h1>
        <p className="mt-1 text-muted-foreground">
          Manage amenities, safety devices, and considerations available to landlords when
          creating listings.
        </p>
      </div>

      <Tabs value={tab} onValueChange={setTab}>
        <TabsList>
          <TabsTrigger value="amenities">
            <Sparkles className="mr-1.5 h-4 w-4" />
            Amenities
          </TabsTrigger>
          <TabsTrigger value="safety">
            <ShieldCheck className="mr-1.5 h-4 w-4" />
            Safety Devices
          </TabsTrigger>
          <TabsTrigger value="considerations">
            <AlertTriangle className="mr-1.5 h-4 w-4" />
            Considerations
          </TabsTrigger>
        </TabsList>

        <TabsContent value="amenities">
          <AmenitiesTab />
        </TabsContent>
        <TabsContent value="safety">
          <SafetyDevicesTab />
        </TabsContent>
        <TabsContent value="considerations">
          <ConsiderationsTab />
        </TabsContent>
      </Tabs>
    </div>
  );
};

// ─── Amenities Tab ──────────────────────────────────────────

function AmenitiesTab() {
  const [items, setItems] = useState<AmenityDefinitionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<AmenityDefinitionDto | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await listingApi.adminGetAmenities();
      setItems(data);
    } catch {
      setError("Failed to load amenities.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const filtered = search.trim()
    ? items.filter(
        (a) =>
          a.name.toLowerCase().includes(search.toLowerCase()) ||
          a.category.toLowerCase().includes(search.toLowerCase()),
      )
    : items;

  const handleSaved = () => {
    setDialogOpen(false);
    setEditing(null);
    void load();
  };

  return (
    <Card>
      <CardHeader className="pb-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <CardTitle className="text-lg">Amenities ({items.length})</CardTitle>
          <div className="flex items-center gap-2">
            <div className="relative w-full sm:w-56">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search amenities..."
                className="pl-9"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <Button
              variant="accent"
              size="sm"
              onClick={() => {
                setEditing(null);
                setDialogOpen(true);
              }}
            >
              <Plus className="mr-1 h-4 w-4" />
              Add
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {loading ? (
          <Loader label="Loading amenities..." />
        ) : error ? (
          <p className="py-8 text-center text-destructive">{error}</p>
        ) : filtered.length === 0 ? (
          <p className="py-8 text-center text-muted-foreground">No amenities found.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Icon</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Category</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>
                    <DynamicIcon iconKey={item.iconKey} className="h-5 w-5 text-muted-foreground" />
                  </TableCell>
                  <TableCell className="font-medium">{item.name}</TableCell>
                  <TableCell>
                    <Badge variant="secondary">{item.category}</Badge>
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        setEditing(item);
                        setDialogOpen(true);
                      }}
                    >
                      <Pencil className="h-4 w-4" />
                      Edit
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <AmenityFormDialog
          existing={editing}
          onSaved={handleSaved}
          onCancel={() => {
            setDialogOpen(false);
            setEditing(null);
          }}
        />
      </Dialog>
    </Card>
  );
}

function AmenityFormDialog({
  existing,
  onSaved,
  onCancel,
}: {
  existing: AmenityDefinitionDto | null;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const isEdit = Boolean(existing);
  const [name, setName] = useState(existing?.name ?? "");
  const [category, setCategory] = useState<AmenityCategory>(existing?.category ?? "Kitchen");
  const [iconKey, setIconKey] = useState(existing?.iconKey ?? "");
  const [sortOrder, setSortOrder] = useState(0);
  const [isActive, setIsActive] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setName(existing?.name ?? "");
    setCategory(existing?.category ?? "Kitchen");
    setIconKey(existing?.iconKey ?? "");
    setSortOrder(0);
    setIsActive(true);
    setError(null);
  }, [existing]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !iconKey.trim()) return;
    setSaving(true);
    setError(null);
    try {
      if (isEdit && existing) {
        const payload: UpdateAmenityDefinitionRequest = {
          name: name.trim(),
          category,
          iconKey: iconKey.trim(),
          isActive,
          sortOrder,
        };
        await listingApi.adminUpdateAmenity(existing.id, payload);
      } else {
        const payload: CreateAmenityDefinitionRequest = {
          name: name.trim(),
          category,
          iconKey: iconKey.trim(),
          sortOrder,
        };
        await listingApi.adminCreateAmenity(payload);
      }
      onSaved();
    } catch {
      setError("Failed to save amenity.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <DialogContent>
      <DialogHeader>
        <DialogTitle>{isEdit ? "Edit Amenity" : "Add Amenity"}</DialogTitle>
      </DialogHeader>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="amenity-name">Name</Label>
          <Input
            id="amenity-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Air conditioning"
            required
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="amenity-category">Category</Label>
          <Select
            id="amenity-category"
            value={category}
            onChange={(e) => setCategory(e.target.value as AmenityCategory)}
          >
            {amenityCategories.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="amenity-icon">Icon key (lucide icon name)</Label>
          <div className="flex items-center gap-3">
            <Input
              id="amenity-icon"
              value={iconKey}
              onChange={(e) => setIconKey(e.target.value)}
              placeholder="e.g. Snowflake"
              required
            />
            {iconKey && (
              <DynamicIcon iconKey={iconKey} className="h-5 w-5 text-muted-foreground shrink-0" />
            )}
          </div>
        </div>
        <div className="space-y-2">
          <Label htmlFor="amenity-sort">Sort order</Label>
          <Input
            id="amenity-sort"
            type="number"
            value={sortOrder}
            onChange={(e) => setSortOrder(Number(e.target.value))}
          />
        </div>
        {isEdit && (
          <div className="flex items-center gap-2">
            <input
              id="amenity-active"
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="h-4 w-4 rounded border-input accent-accent"
            />
            <Label htmlFor="amenity-active">Active</Label>
          </div>
        )}
        {error && <p className="text-sm text-destructive">{error}</p>}
        <DialogFooter>
          <Button type="button" variant="outline" onClick={onCancel} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" variant="accent" disabled={saving || !name.trim() || !iconKey.trim()}>
            {saving ? "Saving..." : isEdit ? "Update" : "Create"}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  );
}

// ─── Safety Devices Tab ─────────────────────────────────────

function SafetyDevicesTab() {
  const [items, setItems] = useState<SafetyDeviceDefinitionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<SafetyDeviceDefinitionDto | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await listingApi.adminGetSafetyDevices();
      setItems(data);
    } catch {
      setError("Failed to load safety devices.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const filtered = search.trim()
    ? items.filter((a) => a.name.toLowerCase().includes(search.toLowerCase()))
    : items;

  const handleSaved = () => {
    setDialogOpen(false);
    setEditing(null);
    void load();
  };

  return (
    <Card>
      <CardHeader className="pb-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <CardTitle className="text-lg">Safety Devices ({items.length})</CardTitle>
          <div className="flex items-center gap-2">
            <div className="relative w-full sm:w-56">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search devices..."
                className="pl-9"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <Button
              variant="accent"
              size="sm"
              onClick={() => {
                setEditing(null);
                setDialogOpen(true);
              }}
            >
              <Plus className="mr-1 h-4 w-4" />
              Add
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {loading ? (
          <Loader label="Loading safety devices..." />
        ) : error ? (
          <p className="py-8 text-center text-destructive">{error}</p>
        ) : filtered.length === 0 ? (
          <p className="py-8 text-center text-muted-foreground">No safety devices found.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Icon</TableHead>
                <TableHead>Name</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>
                    <DynamicIcon iconKey={item.iconKey} className="h-5 w-5 text-muted-foreground" />
                  </TableCell>
                  <TableCell className="font-medium">{item.name}</TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        setEditing(item);
                        setDialogOpen(true);
                      }}
                    >
                      <Pencil className="h-4 w-4" />
                      Edit
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <SimpleDefinitionFormDialog
          kind="Safety Device"
          existing={editing}
          onSaved={handleSaved}
          onCancel={() => {
            setDialogOpen(false);
            setEditing(null);
          }}
          onCreate={(p) => listingApi.adminCreateSafetyDevice(p)}
          onUpdate={(id, p) => listingApi.adminUpdateSafetyDevice(id, p)}
        />
      </Dialog>
    </Card>
  );
}

// ─── Considerations Tab ─────────────────────────────────────

function ConsiderationsTab() {
  const [items, setItems] = useState<ConsiderationDefinitionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editing, setEditing] = useState<ConsiderationDefinitionDto | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await listingApi.adminGetConsiderations();
      setItems(data);
    } catch {
      setError("Failed to load considerations.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const filtered = search.trim()
    ? items.filter((a) => a.name.toLowerCase().includes(search.toLowerCase()))
    : items;

  const handleSaved = () => {
    setDialogOpen(false);
    setEditing(null);
    void load();
  };

  return (
    <Card>
      <CardHeader className="pb-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <CardTitle className="text-lg">Considerations ({items.length})</CardTitle>
          <div className="flex items-center gap-2">
            <div className="relative w-full sm:w-56">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search considerations..."
                className="pl-9"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <Button
              variant="accent"
              size="sm"
              onClick={() => {
                setEditing(null);
                setDialogOpen(true);
              }}
            >
              <Plus className="mr-1 h-4 w-4" />
              Add
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        {loading ? (
          <Loader label="Loading considerations..." />
        ) : error ? (
          <p className="py-8 text-center text-destructive">{error}</p>
        ) : filtered.length === 0 ? (
          <p className="py-8 text-center text-muted-foreground">No considerations found.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Icon</TableHead>
                <TableHead>Name</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>
                    <DynamicIcon iconKey={item.iconKey} className="h-5 w-5 text-muted-foreground" />
                  </TableCell>
                  <TableCell className="font-medium">{item.name}</TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        setEditing(item);
                        setDialogOpen(true);
                      }}
                    >
                      <Pencil className="h-4 w-4" />
                      Edit
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <SimpleDefinitionFormDialog
          kind="Consideration"
          existing={editing}
          onSaved={handleSaved}
          onCancel={() => {
            setDialogOpen(false);
            setEditing(null);
          }}
          onCreate={(p) => listingApi.adminCreateConsideration(p)}
          onUpdate={(id, p) => listingApi.adminUpdateConsideration(id, p)}
        />
      </Dialog>
    </Card>
  );
}

// ─── Shared form for Safety Devices / Considerations ────────

type SimpleDefinition = { id: string; name: string; iconKey: string };

function SimpleDefinitionFormDialog<
  T extends SimpleDefinition,
  C extends CreateSafetyDeviceDefinitionRequest | CreateConsiderationDefinitionRequest,
  U extends UpdateSafetyDeviceDefinitionRequest | UpdateConsiderationDefinitionRequest,
>({
  kind,
  existing,
  onSaved,
  onCancel,
  onCreate,
  onUpdate,
}: {
  kind: string;
  existing: T | null;
  onSaved: () => void;
  onCancel: () => void;
  onCreate: (p: C) => Promise<unknown>;
  onUpdate: (id: string, p: U) => Promise<unknown>;
}) {
  const isEdit = Boolean(existing);
  const [name, setName] = useState(existing?.name ?? "");
  const [iconKey, setIconKey] = useState(existing?.iconKey ?? "");
  const [sortOrder, setSortOrder] = useState(0);
  const [isActive, setIsActive] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setName(existing?.name ?? "");
    setIconKey(existing?.iconKey ?? "");
    setSortOrder(0);
    setIsActive(true);
    setError(null);
  }, [existing]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !iconKey.trim()) return;
    setSaving(true);
    setError(null);
    try {
      if (isEdit && existing) {
        await onUpdate(existing.id, {
          name: name.trim(),
          iconKey: iconKey.trim(),
          isActive,
          sortOrder,
        } as U);
      } else {
        await onCreate({
          name: name.trim(),
          iconKey: iconKey.trim(),
          sortOrder,
        } as C);
      }
      onSaved();
    } catch {
      setError(`Failed to save ${kind.toLowerCase()}.`);
    } finally {
      setSaving(false);
    }
  };

  return (
    <DialogContent>
      <DialogHeader>
        <DialogTitle>{isEdit ? `Edit ${kind}` : `Add ${kind}`}</DialogTitle>
      </DialogHeader>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="def-name">Name</Label>
          <Input
            id="def-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder={`e.g. Smoke detector`}
            required
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="def-icon">Icon key (lucide icon name)</Label>
          <div className="flex items-center gap-3">
            <Input
              id="def-icon"
              value={iconKey}
              onChange={(e) => setIconKey(e.target.value)}
              placeholder="e.g. Flame"
              required
            />
            {iconKey && (
              <DynamicIcon iconKey={iconKey} className="h-5 w-5 text-muted-foreground shrink-0" />
            )}
          </div>
        </div>
        <div className="space-y-2">
          <Label htmlFor="def-sort">Sort order</Label>
          <Input
            id="def-sort"
            type="number"
            value={sortOrder}
            onChange={(e) => setSortOrder(Number(e.target.value))}
          />
        </div>
        {isEdit && (
          <div className="flex items-center gap-2">
            <input
              id="def-active"
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="h-4 w-4 rounded border-input accent-accent"
            />
            <Label htmlFor="def-active">Active</Label>
          </div>
        )}
        {error && <p className="text-sm text-destructive">{error}</p>}
        <DialogFooter>
          <Button type="button" variant="outline" onClick={onCancel} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" variant="accent" disabled={saving || !name.trim() || !iconKey.trim()}>
            {saving ? "Saving..." : isEdit ? "Update" : "Create"}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  );
}
