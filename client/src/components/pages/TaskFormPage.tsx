import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Controller, useForm, type Resolver } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import {
  TASK_STATUS_OPTIONS,
  TaskStatus,
  type CreateTaskDto,
  type UpdateTaskDto,
} from "../../app/types/Task";
import {
  useCreateTask,
  useTask,
  useUpdateTask,
} from "../../app/hooks/useTasks";
import Input from "../atoms/Input";
import TextArea from "../atoms/TextArea";
import Autocomplete, {
  type AutocompleteOption,
} from "../molecules/Autocomplete";
import { useUser, useUsers } from "../../app/hooks/useUsers";
import { useDebounce } from "../../app/hooks/useDebounce";
import CloseIcon from "../../assets/icons/close.svg?react";
import { useCreateNotification } from "../../app/hooks/useNotification";

const schema = yup.object({
  title: yup
    .string()
    .required("Title is required")
    .min(1)
    .max(100, "Max 100 characters"),
  description: yup
    .string()
    .max(500, "Max 500 characters")
    .nullable()
    .notRequired(),
  dueDate: yup
    .string()
    .nullable()
    .transform((v) => (v === "" ? null : v))
    .notRequired()
    .test("is-date-or-null", "Invalid date", (value) => {
      if (!value) return true;
      return !Number.isNaN(Date.parse(value));
    }),
  assignedToExternalId: yup.string().nullable().notRequired(),
  itemStatus: yup
    .number()
    .oneOf([TaskStatus.Pending, TaskStatus.InProgress, TaskStatus.Done])
    .required(),
});

type FormValues = yup.Asserts<typeof schema>;

const TaskFormPage: React.FC = () => {
  const { id } = useParams<{ id?: string }>();
  const minChars = 2;
  const [searchTerm, setSearchTerm] = useState("");

  const defaultValues: FormValues = {
    title: "",
    description: null,
    dueDate: null,
    assignedToExternalId: null,
    itemStatus: TaskStatus.Pending,
  };

  const navigate = useNavigate();
  const { register, handleSubmit, formState, reset, control, watch } =
    useForm<FormValues>({
      resolver: yupResolver(schema) as Resolver<FormValues>,
      defaultValues,
    });

  const isEdit = Boolean(id);
  const assignedToExternalId = watch("assignedToExternalId");

  const { errors, isSubmitting } = formState;
  const taskQuery = useTask(id);
  const userQuery = useUser(taskQuery.data?.assignedTo?.externalId || "");
  const createMutation = useCreateTask();
  const updateMutation = useUpdateTask();

  const notificationMutation = useCreateNotification();

  const debouncedSearch = useDebounce(searchTerm, 400);
  const usersQuery = useUsers(
    { search: debouncedSearch, page: 1, pageSize: 20 },
    { enabled: (debouncedSearch?.length ?? 0) >= minChars }
  );

  const options: AutocompleteOption[] = useMemo(
    () =>
      (usersQuery?.data?.items ?? []).map((u) => ({
        id: u.id,
        name: u.name,
        email: u.email,
        externalId: u.externalId,
      })),
    [usersQuery?.data]
  );
  const selectedOption = useMemo(
    () => options.find((o) => o.externalId === assignedToExternalId) ?? null,
    [options, assignedToExternalId]
  );

  useEffect(() => {
    if (isEdit && taskQuery.data) {
      const t = taskQuery.data;
      reset({
        title: t.title ?? "",
        description: t.description ?? null,
        dueDate: t.dueDate
          ? new Date(t.dueDate).toISOString().slice(0, 10)
          : null,
        assignedToExternalId: t.assignedTo?.externalId ?? null,
        itemStatus:
          typeof t.status === "number" ? t.status : TaskStatus.Pending,
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isEdit, taskQuery.data]);

  useEffect(() => {
    if (!isEdit) {
      reset(defaultValues);

      setSearchTerm("");
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isEdit]);

  const onSubmit = async (values: FormValues) => {
    const payloadCreate: CreateTaskDto = {
      title: values.title.trim(),
      description: values.description ?? null,
      dueDate: values.dueDate ? new Date(values.dueDate).toISOString() : null,
      assignedToExternalId: values.assignedToExternalId ?? null,
    };

    const payloadUpdate: UpdateTaskDto = {
      title: values.title.trim(),
      description: values.description ?? null,
      dueDate: values.dueDate ? new Date(values.dueDate).toISOString() : null,
      itemStatus: TASK_STATUS_OPTIONS[values.itemStatus].value,
      assignedToExternalId: values.assignedToExternalId ?? null,
    };

    try {
      if (isEdit && id) {
        await updateMutation.mutateAsync({ id, dto: payloadUpdate });
        await notificationMutation.mutateAsync({
          recipientUserId: values.assignedToExternalId || "",
          title: `Task Updated: ${values.title}`,
          message: `The task "${values.title}" has been updated.`,
          type: 1,
          relatedTaskId: id,
        });
        navigate(-1);
      } else {
        const result = await createMutation.mutateAsync(payloadCreate);
        await notificationMutation.mutateAsync({
          recipientUserId: values.assignedToExternalId || "",
          title: `Task Created: ${values.title}`,
          message: `The task "${values.title}" has been created.`,
          type: 0,
          relatedTaskId: result.id,
        });
        navigate("/");
      }
    } catch (err: unknown) {
      console.error("Submit failed", err);
    }
  };

  if (isEdit && taskQuery.isLoading) {
    return (
      <div className=" h-full flex flex-col items-center justify-center gap-4">
        <p className="text-2xl font-bold text-primary">Loading...</p>
        <progress className="progress progress-primary w-56"></progress>
      </div>
    );
  }

  if (isEdit && taskQuery.isError) {
    return <div className="p-6 text-red-600">Failed to load task.</div>;
  }

  return (
    <div className="max-w-xl mx-auto p-6 h-full flex flex-col flex-grow">
      <div>
        <h1 className="text-2xl font-semibold mb-4">
          {isEdit ? "Edit Task" : "Create Task"}
        </h1>
      </div>

      <form
        onSubmit={handleSubmit(onSubmit)}
        className="space-y-4 flex flex-col flex-grow"
      >
        <div className="flex flex-col flex-grow gap-4">
          <Input
            label="Title"
            placeholder="Enter task title"
            {...register("title")}
            error={errors.title?.message as string | undefined}
          />

          <TextArea
            label="Description"
            placeholder="Optional description"
            rows={5}
            {...register("description")}
            error={errors.description?.message as string | undefined}
          />

          <Input
            label="Due Date"
            type="date"
            {...register("dueDate")}
            error={errors.dueDate?.message as string | undefined}
          />

          <Controller
            name="assignedToExternalId"
            control={control}
            defaultValue={null}
            render={({ field, fieldState }) => (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Assigned To
                </label>
                {assignedToExternalId ? (
                  <div className="flex gap-2">
                    <h3>{userQuery.data?.name || selectedOption?.name}</h3>
                    <button
                      className=""
                      onClick={() => {
                        field.onChange(null);
                        setSearchTerm("");
                      }}
                    >
                      <CloseIcon />
                    </button>
                  </div>
                ) : (
                  <Autocomplete
                    value={selectedOption}
                    onChange={(option) =>
                      field.onChange(option ? option.externalId ?? null : null)
                    }
                    onInputChange={setSearchTerm}
                    options={options || []}
                    placeholder="Type to search users..."
                    minChars={minChars}
                    isLoading={usersQuery.isLoading}
                  />
                )}

                {fieldState.error && (
                  <p className="text-sm text-red-500 mt-1">
                    {fieldState.error.message}
                  </p>
                )}
              </div>
            )}
          />

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Status
            </label>

            <Controller
              name="itemStatus"
              control={control}
              render={({ field }) => (
                <div className="flex flex-col gap-2">
                  {TASK_STATUS_OPTIONS.map((option) => (
                    <label
                      key={option.value}
                      className="inline-flex items-center cursor-pointer"
                    >
                      <input
                        type="radio"
                        name="itemStatus"
                        value={String(option.value)}
                        checked={field.value === option.value}
                        onChange={() => field.onChange(option.value)}
                        className="form-radio text-blue-600"
                      />
                      <span className="ml-2">{option.label}</span>
                    </label>
                  ))}
                </div>
              )}
            />
          </div>

          {(createMutation.isError || updateMutation.isError) && (
            <div className="text-red-600">
              {(createMutation.error as Error)?.message ??
                (updateMutation.error as Error)?.message ??
                "An error occurred."}
            </div>
          )}
        </div>

        <div className="flex gap-2 justify-end">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={() => {
              if (isEdit && id) {
                if (taskQuery.data) {
                  reset({
                    title: taskQuery.data.title ?? "",
                    description: taskQuery.data.description ?? null,
                    dueDate: taskQuery.data.dueDate
                      ? new Date(taskQuery.data.dueDate)
                          .toISOString()
                          .slice(0, 10)
                      : null,
                    assignedToExternalId:
                      taskQuery.data.assignedTo?.externalId ?? null,
                    itemStatus:
                      typeof taskQuery.data.status === "number"
                        ? taskQuery.data.status
                        : TaskStatus.Pending,
                  });
                }
              } else {
                reset();
              }
            }}
            disabled={
              isSubmitting ||
              createMutation.isPending ||
              updateMutation.isPending
            }
          >
            Reset
          </button>

          <button
            type="submit"
            className="btn btn-primary"
            disabled={
              isSubmitting ||
              createMutation.isPending ||
              updateMutation.isPending
            }
          >
            {isEdit
              ? updateMutation.isPending
                ? "Saving..."
                : "Save changes"
              : createMutation.isPending
              ? "Creating..."
              : "Create Task"}
          </button>
        </div>
      </form>
    </div>
  );
};

export default TaskFormPage;
