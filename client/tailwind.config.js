/** @type {import('tailwindcss').Config} */

module.exports =  {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      fontFamily: {
        fjalla: ['"Fjalla One"', 'sans-serif'],
      },
    }
  },
  plugins: [],
}
