import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { HlmCardImports } from '@spartan-ng/helm/card';

@Component({
  selector: 'app-dashboard-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe, HlmCardImports],
  template: `
    <div class="space-y-6 p-4 md:p-6">
      <div>
        <h1 class="text-3xl font-bold">{{ 'dashboard.title' | transloco }}</h1>
        <p class="text-muted-foreground">{{ 'dashboard.welcome' | transloco }}</p>
      </div>

      <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <div hlmCard size="sm">
          <div hlmCardHeader>
            <div hlmCardTitle>{{ 'dashboard.stats.totalProperties' | transloco }}</div>
          </div>
          <div hlmCardContent>
            <div class="text-2xl font-bold">42</div>
            <p class="text-xs text-muted-foreground">{{ 'dashboard.stats.propertiesChange' | transloco: {count: 2} }}</p>
          </div>
        </div>

        <div hlmCard size="sm">
          <div hlmCardHeader>
            <div hlmCardTitle>{{ 'dashboard.stats.activeLeases' | transloco }}</div>
          </div>
          <div hlmCardContent>
            <div class="text-2xl font-bold">38</div>
            <p class="text-xs text-muted-foreground">{{ 'dashboard.stats.occupancyRate' | transloco: {rate: 90} }}</p>
          </div>
        </div>

        <div hlmCard size="sm">
          <div hlmCardHeader>
            <div hlmCardTitle>{{ 'dashboard.stats.pendingRequests' | transloco }}</div>
          </div>
          <div hlmCardContent>
            <div class="text-2xl font-bold">7</div>
            <p class="text-xs text-muted-foreground">{{ 'dashboard.stats.urgentCount' | transloco: {count: 2} }}</p>
          </div>
        </div>

        <div hlmCard size="sm">
          <div hlmCardHeader>
            <div hlmCardTitle>{{ 'dashboard.stats.revenueMTD' | transloco }}</div>
          </div>
          <div hlmCardContent>
            <div class="text-2xl font-bold">$45,231</div>
            <p class="text-xs text-muted-foreground">{{ 'dashboard.stats.revenueChange' | transloco: {percent: 12} }}</p>
          </div>
        </div>
      </div>

      <div hlmCard>
        <div hlmCardHeader>
          <div hlmCardTitle>{{ 'dashboard.activity.title' | transloco }}</div>
          <div hlmCardDescription>{{ 'dashboard.activity.description' | transloco }}</div>
        </div>
        <div hlmCardContent>
          <div class="space-y-4">
            <div class="flex items-center gap-4">
              <div class="h-2 w-2 rounded-full bg-green-500" aria-hidden="true"></div>
              <div class="flex-1">
                <p class="text-sm font-medium">{{ 'dashboard.activity.leaseSigned' | transloco: {unit: '4B'} }}</p>
                <p class="text-xs text-muted-foreground">{{ 'dashboard.activity.hoursAgo' | transloco: {hours: 2} }}</p>
              </div>
            </div>
            <div class="flex items-center gap-4">
              <div class="h-2 w-2 rounded-full bg-yellow-500" aria-hidden="true"></div>
              <div class="flex-1">
                <p class="text-sm font-medium">{{ 'dashboard.activity.maintenanceRequest' | transloco: {location: 'Building A'} }}</p>
                <p class="text-xs text-muted-foreground">{{ 'dashboard.activity.hoursAgo' | transloco: {hours: 5} }}</p>
              </div>
            </div>
            <div class="flex items-center gap-4">
              <div class="h-2 w-2 rounded-full bg-blue-500" aria-hidden="true"></div>
              <div class="flex-1">
                <p class="text-sm font-medium">{{ 'dashboard.activity.paymentReceived' | transloco: {amount: '\$2,500'} }}</p>
                <p class="text-xs text-muted-foreground">{{ 'dashboard.activity.daysAgo' | transloco: {days: 1} }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class DashboardHomePage {}
