import React, { forwardRef } from "react";
import SearchIcon from "../../assets/icons/search.svg?react";
export interface SearchInputProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, "type"> {
  onSearch?: (value: string) => void;
  containerClassName?: string;
}

const SearchInput = forwardRef<HTMLInputElement, SearchInputProps>(
  (
    { onSearch, placeholder = "Search", containerClassName = "", ...props },
    ref
  ) => {
    const handleSubmit = (e: React.FormEvent) => {
      e.preventDefault();
      const value =
        (e.target as HTMLFormElement).querySelector("input")?.value ?? "";
      onSearch?.(value);
    };

    return (
      <form onSubmit={handleSubmit} className={containerClassName}>
        <label className="input bg-base-100 w-full rounded-xl flex items-center gap-2">
          <SearchIcon />
          <input
            ref={ref}
            type="search"
            inputMode="search"
            enterKeyHint="search"
            placeholder={placeholder}
            className="w-full bg-transparent outline-none"
            {...props}
          />
        </label>
      </form>
    );
  }
);

SearchInput.displayName = "SearchInput";

export default SearchInput;
