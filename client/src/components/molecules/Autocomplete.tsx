import React, { useState, useRef, useEffect, forwardRef } from "react";
import Input from "../atoms/Input";
import type { InputProps } from "../atoms/Input";

export interface AutocompleteOption {
  id: string;
  name: string;
  [key: string]: any;
}

export interface AutocompleteProps
  extends Omit<InputProps, "value" | "onChange"> {
  value?: AutocompleteOption | null;
  options: AutocompleteOption[];
  onChange?: (option: AutocompleteOption | null) => void;
  onInputChange?: (value: string) => void;
  placeholder?: string;
  minChars?: number;
  isLoading?: boolean;
}

const Autocomplete = forwardRef<HTMLInputElement, AutocompleteProps>(
  (
    {
      value = null,
      options = [],
      onChange,
      onInputChange,
      placeholder = "Search...",
      minChars = 2,
      isLoading = false,
      ...inputProps
    },
    ref
  ) => {
    const [inputValue, setInputValue] = useState(value?.name || "");
    const [isOpen, setIsOpen] = useState(false);
    const [filteredOptions, setFilteredOptions] = useState<
      AutocompleteOption[]
    >([]);
    const containerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
      if (value?.name !== undefined && value?.name !== inputValue) {
        setInputValue(value?.name ?? "");
      }
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [value]);

    useEffect(() => {
      if (inputValue.length >= minChars) {
        const term = inputValue.toLowerCase();
        const results = options.filter((opt) =>
          opt.name.toLowerCase().includes(term)
        );
        setFilteredOptions(results);
        setIsOpen((results.length > 0 || isLoading) && !value);
      } else {
        setFilteredOptions([]);
        setIsOpen(isLoading);
      }
    }, [inputValue, options, minChars, isLoading]);

    useEffect(() => {
      const handleClickOutside = (e: MouseEvent) => {
        if (
          containerRef.current &&
          !containerRef.current.contains(e.target as Node)
        ) {
          setIsOpen(false);
        }
      };
      document.addEventListener("mousedown", handleClickOutside);
      return () =>
        document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    const handleSelect = (option: AutocompleteOption) => {
      setInputValue(option.name);
      setIsOpen(false);
      onChange?.(option);
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const val = e.target.value;
      setInputValue(val);
      onInputChange?.(val);
      if (!val) {
        onChange?.(null);
      }
    };

    const handleClear = () => {
      setInputValue("");
      onChange?.(null);
      onInputChange?.("");
      setIsOpen(false);
    };

    return (
      <div className="relative" ref={containerRef}>
        <div className="relative">
          <Input
            ref={ref}
            value={inputValue}
            onChange={handleInputChange}
            placeholder={placeholder}
            {...(inputProps as any)}
          />
          {inputValue && (
            <button
              type="button"
              className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
              onClick={handleClear}
            >
              ×
            </button>
          )}
        </div>

        {isOpen && (
          <ul className="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md bg-white py-1 text-base shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none sm:text-sm">
            {isLoading ? (
              <li className="relative select-none px-4 py-2 text-gray-700 flex justify-center">
                <span className="loading loading-dots loading-xl"></span>
              </li>
            ) : filteredOptions.length === 0 ? (
              <li className="relative select-none px-4 py-2 text-gray-700">
                No results found
              </li>
            ) : (
              filteredOptions.map((option) => (
                <li
                  key={option.id}
                  className="relative cursor-pointer select-none px-4 py-2 text-gray-900 hover:bg-primary hover:text-white"
                  onClick={() => handleSelect(option)}
                >
                  <div className="font-medium">{option.name}</div>
                  {option.email && (
                    <div className="text-xs text-gray-500">{option.email}</div>
                  )}
                </li>
              ))
            )}
          </ul>
        )}
      </div>
    );
  }
);

Autocomplete.displayName = "Autocomplete";

export default Autocomplete;
