/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{vue,js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        drivolution: {
          50: '#eff7ff',
          100: '#dceeff',
          300: '#7cc4ff',
          500: '#0877d8',
          600: '#0877d8',
          700: '#065aa7',
          900: '#0b2948'
        }
      }
    },
  },
  plugins: [],
}
