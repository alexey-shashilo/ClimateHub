namespace ClimateHub.Modules.IAM.Domain;

public enum Permission
{
    building_read,
    building_configure,
    room_read,
    room_configure,
    device_read,
    device_register,
    device_configure,
    device_delete,
    command_create,
    command_read,
    command_cancel,
    environment_read,
    engineering_read,
    engineering_configure,
    need_read,
    need_configure,
    need_execute,
    policy_read,
    policy_configure,
    user_read,
    user_configure,
    audit_read,
    admin,
    sse_subscribe,
    report_read,
    report_create,
    maintenance_read,
    maintenance_configure
}