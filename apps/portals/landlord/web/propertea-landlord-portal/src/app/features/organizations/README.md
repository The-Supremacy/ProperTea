# Organizations Feature

## Overview
Organization registration, management, and configuration for landlords.

## Features
- Organization registration with admin user setup
- Real-time organization name availability checking
- Password strength validation with visual feedback
- Multi-language support via Transloco
- Form validation with translated error messages
- Auto-redirect to dashboard after successful registration

## Business Rules
- Organization names must be at least 2 characters and unique
- Passwords must meet complexity requirements (uppercase, lowercase, number, special character)
- Registration creates both organization and admin user in single transaction
- Name availability is debounced (500ms) to reduce API calls
