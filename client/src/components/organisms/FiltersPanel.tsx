import React, { useState } from "react";
import {
  SCOPE_OPTIONS,
  TASK_STATUS_OPTIONS,
  TaskStatus,
} from "../../app/types/Task";

type FilterParams = {
  status?: TaskStatus;
  scope?: string;
};

type Props = {
  initialFilters?: FilterParams;
  onApply: (filters: FilterParams) => void;
  onClose: () => void;
};

const FiltersPanel: React.FC<Props> = ({
  initialFilters,
  onApply,
  onClose,
}) => {
  const [status, setStatus] = useState<number | undefined>(
    initialFilters?.status !== undefined
      ? Number(initialFilters.status)
      : undefined
  );
  const [scope, setScope] = useState<string | undefined>(initialFilters?.scope);

  const clearFilters = () => {
    setStatus(undefined);
    setScope(undefined);
  };

  const handleApply = () => {
    onApply({
      status:
        status !== undefined ? TASK_STATUS_OPTIONS[status].value : undefined,
      scope,
    });
    onClose();
  };

  return (
    <div className="fixed left-0 bottom-18 z-50 flex justify-end items-end w-full h-full">
      <div
        className="bg-neutral opacity-80 w-full h-full absolute -z-40"
        onClick={() => onClose()}
      ></div>
      <div className="bg-base-300 p-6 flex w-full h-[60%] flex-1 flex-col shadow-lg rounded-t-2xl">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-lg font-semibold">Filters</h2>
          <button
            onClick={onClose}
            aria-label="Close filters"
            className="text-gray-600 hover:text-gray-900"
          >
            ✕
          </button>
        </div>

        <div className="mb-6">
          <h3 className="font-medium mb-2">Status</h3>
          <div className="flex flex-col gap-2">
            {TASK_STATUS_OPTIONS.map((option) => (
              <label
                key={option.value}
                className="inline-flex items-center cursor-pointer"
              >
                <input
                  type="radio"
                  name="status"
                  value={String(option.value)}
                  checked={status === option.value}
                  onChange={() => setStatus(option.value)}
                  className="form-radio text-blue-600"
                />
                <span className="ml-2">{option.label}</span>
              </label>
            ))}
            <label className="inline-flex items-center cursor-pointer">
              <input
                type="radio"
                name="status"
                value=""
                checked={status === undefined}
                onChange={() => setStatus(undefined)}
                className="form-radio text-blue-600"
              />
              <span className="ml-2">Any</span>
            </label>
          </div>
        </div>

        <div className="mb-6">
          <h3 className="font-medium mb-2">Scope</h3>
          <div className="flex flex-col gap-2">
            {SCOPE_OPTIONS.map((option) => (
              <label
                key={option}
                className="inline-flex items-center cursor-pointer"
              >
                <input
                  type="radio"
                  name="scope"
                  value={option}
                  checked={scope === option}
                  onChange={() => setScope(option)}
                  className="form-radio text-blue-600"
                />
                <span className="ml-2 capitalize">{option}</span>
              </label>
            ))}
            <label className="inline-flex items-center cursor-pointer">
              <input
                type="radio"
                name="scope"
                value=""
                checked={scope === undefined}
                onChange={() => setScope(undefined)}
                className="form-radio text-blue-600"
              />
              <span className="ml-2">Any</span>
            </label>
          </div>
        </div>

        <div className="mt-auto flex justify-between items-center">
          <button
            onClick={clearFilters}
            className="text-sm text-gray-600 hover:underline"
            type="button"
          >
            Clear all
          </button>
          <button
            onClick={handleApply}
            className="bg-black text-white px-4 py-2 rounded-md hover:bg-gray-900"
            type="button"
          >
            Apply
          </button>
        </div>
      </div>
    </div>
  );
};

export default FiltersPanel;
